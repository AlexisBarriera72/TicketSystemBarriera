using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Shared.Dtos;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

public sealed class NotificationFeedService(IDbContextFactory<ApplicationDbContext> dbFactory)
    : INotificationFeedService
{
    public async Task<List<NotificationItemDto>> GetFeedAsync(string userId, bool isStaff, int take = 30)
    {
        take = Math.Clamp(take, 1, 100);
        var cutoff = DateTime.UtcNow.AddDays(-14);
        using var db = dbFactory.CreateDbContext();
        var items = new List<NotificationItemDto>();

        // --- Órdenes: para conductor/cliente, las suyas activas ("asignadas");
        //     para la oficina, las solicitudes nuevas sin gestionar ---
        if (isStaff)
        {
            var requests = await db.Orders.Include(o => o.Category)
                .Where(o => o.Status == OrderStatus.Requested && o.CreatedAt > cutoff)
                .OrderByDescending(o => o.CreatedAt).Take(take).ToListAsync();
            items.AddRange(requests.Select(o => new NotificationItemDto(
                "assignment", $"Nueva solicitud · Orden #{o.Id}",
                $"{o.Title} — {o.Category?.Name}", o.CreatedAt, OrderId: o.Id)));
        }
        else
        {
            var mine = await db.Orders.Include(o => o.Category)
                .Where(o => (o.AssignedDriverId == userId || o.AuthorId == userId)
                            && o.Status != OrderStatus.Completed
                            && (o.UpdatedAt ?? o.CreatedAt) > cutoff)
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt).Take(take).ToListAsync();
            items.AddRange(mine.Select(o => new NotificationItemDto(
                "assignment", $"Orden #{o.Id} · {o.Title}",
                $"{o.Category?.Name} — {StatusLabel(o.Status)}",
                o.UpdatedAt ?? o.CreatedAt, OrderId: o.Id)));
        }

        // --- Mensajes de chat de orden (de otros, no de sistema) ---
        var msgQuery = db.Messages.Include(m => m.User)
            .Where(m => !m.IsSystem && m.UserId != userId && m.CreatedAt > cutoff);
        if (!isStaff)
        {
            msgQuery = msgQuery.Where(m =>
                m.Order!.AssignedDriverId == userId || m.Order!.AuthorId == userId);
        }
        var messages = await msgQuery.OrderByDescending(m => m.CreatedAt).Take(take).ToListAsync();
        items.AddRange(messages.Select(m => new NotificationItemDto(
            "order-message",
            $"{Name(m.User)} · Orden #{m.OrderId}",
            Preview(m.Content), m.CreatedAt, OrderId: m.OrderId)));

        // --- Mensajes directos (de otros) en mis conversaciones ---
        var myConvos = db.DirectParticipants.Where(p => p.UserId == userId).Select(p => p.ConversationId);
        var dms = await db.DirectMessages.Include(m => m.Sender)
            .Where(m => myConvos.Contains(m.ConversationId) && m.SenderUserId != userId && m.CreatedAt > cutoff)
            .OrderByDescending(m => m.CreatedAt).Take(take).ToListAsync();
        items.AddRange(dms.Select(m => new NotificationItemDto(
            "dm", Name(m.Sender), Preview(m.Content), m.CreatedAt, ConversationId: m.ConversationId)));

        return items.OrderByDescending(i => i.CreatedAtUtc).Take(take).ToList();
    }

    private static string Name(Data.ApplicationUser? u) => u?.DisplayName ?? u?.Email ?? "Alguien";
    private static string Preview(string s) =>
        string.IsNullOrWhiteSpace(s) ? "Envió una foto" : (s.Length <= 120 ? s : s[..120] + "…");

    private static string StatusLabel(OrderStatus s) => s switch
    {
        OrderStatus.Requested => "Solicitada",
        OrderStatus.Assigned => "Asignada",
        OrderStatus.EnRoute => "En camino",
        OrderStatus.InProgress => "En curso",
        OrderStatus.PendingSignature => "Pendiente de firma",
        OrderStatus.Completed => "Completada",
        _ => s.ToString()
    };
}
