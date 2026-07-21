using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

public class DirectMessageService(IDbContextFactory<ApplicationDbContext> dbFactory) : IDirectMessageService
{
    public async Task<bool> IsParticipantAsync(int conversationId, string userId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.DirectParticipants
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId);
    }

    public async Task<(DirectConversation?, string?)> GetOrCreateAsync(string creatorUserId, string otherUserId)
    {
        if (creatorUserId == otherUserId)
        {
            return (null, "No puedes abrir una conversación contigo mismo.");
        }

        using var context = dbFactory.CreateDbContext();
        var other = await context.Users.FindAsync(otherUserId);
        if (other is null) return (null, "El usuario no existe.");

        // Dedupe 1:1: conversación existente con EXACTAMENTE estos dos participantes
        var existing = await context.DirectConversations
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Where(c => c.Participants.Count == 2 &&
                        c.Participants.Any(p => p.UserId == creatorUserId) &&
                        c.Participants.Any(p => p.UserId == otherUserId))
            .FirstOrDefaultAsync();
        if (existing is not null) return (existing, null);

        var conversation = new DirectConversation
        {
            CreatedByUserId = creatorUserId,
            Participants =
            {
                new DirectParticipant { UserId = creatorUserId },
                new DirectParticipant { UserId = otherUserId },
            },
        };
        context.DirectConversations.Add(conversation);
        await context.SaveChangesAsync();

        return await context.DirectConversations
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .FirstAsync(c => c.Id == conversation.Id) is { } created ? (created, null) : (null, "Error al crear.");
    }

    public async Task<List<DirectConversation>> GetMineAsync(string userId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.DirectConversations
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.Id)
            .ToListAsync();
    }

    public async Task<DirectMessage?> GetLastMessageAsync(int conversationId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.DirectMessages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<DirectMessage>> GetMessagesAsync(int conversationId, int take = 50,
        int? beforeId = null, int? afterId = null)
    {
        using var context = dbFactory.CreateDbContext();
        take = Math.Clamp(take, 1, 200);

        var query = context.DirectMessages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId);

        if (afterId is not null)
        {
            return await query.Where(m => m.Id > afterId).OrderBy(m => m.Id).Take(take).ToListAsync();
        }
        if (beforeId is not null)
        {
            query = query.Where(m => m.Id < beforeId);
        }
        var page = await query.OrderByDescending(m => m.Id).Take(take).ToListAsync();
        page.Reverse();
        return page;
    }

    public async Task<(DirectMessage?, string?)> SendAsync(int conversationId, string senderUserId,
        string? senderRole, string content, DateTime? capturedAtUtc, string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(content)) return (null, "El mensaje no puede estar vacío.");

        using var context = dbFactory.CreateDbContext();

        // Defensa en profundidad: pertenencia comprobada también aquí, no solo en el endpoint
        var isParticipant = await context.DirectParticipants
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == senderUserId);
        if (!isParticipant) return (null, "No formas parte de esta conversación.");

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await context.DirectMessages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey);
            if (existing is not null) return (existing, null);
        }

        var message = new DirectMessage
        {
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            SenderRole = senderRole,
            Content = content.Trim(),
            CapturedAtUtc = capturedAtUtc,
            IdempotencyKey = string.IsNullOrEmpty(idempotencyKey) ? null : idempotencyKey,
        };
        context.DirectMessages.Add(message);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException) when (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await context.DirectMessages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey);
            if (existing is not null) return (existing, null);
            throw;
        }
        await context.Entry(message).Reference(m => m.Sender).LoadAsync();
        return (message, null);
    }
}
