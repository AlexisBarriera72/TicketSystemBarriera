using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

public sealed class NotificationService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IPushSender push,
    ILogger<NotificationService> log) : INotificationService
{
    public async Task NotifyOrderMessageAsync(int orderId, string senderUserId, string senderName, string preview)
    {
        await SafeAsync(async () =>
        {
            using var db = dbFactory.CreateDbContext();
            var order = await db.Orders
                .Select(o => new { o.Id, o.AuthorId, o.AssignedDriverId })
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) return;

            // Las dos partes de campo: cliente (autor) y conductor asignado
            var recipients = new[] { order.AuthorId, order.AssignedDriverId };
            await SendToUsersAsync(recipients, senderUserId, new PushMessage(
                Title: string.IsNullOrWhiteSpace(senderName) ? $"Orden #{orderId}" : senderName,
                Body: Trim(preview),
                Data: new Dictionary<string, string> { ["type"] = "order-chat", ["orderId"] = orderId.ToString() }));
        });
    }

    public async Task NotifyDirectMessageAsync(int conversationId, string senderUserId, string senderName, string preview)
    {
        await SafeAsync(async () =>
        {
            using var db = dbFactory.CreateDbContext();
            var participantIds = await db.DirectParticipants
                .Where(p => p.ConversationId == conversationId)
                .Select(p => p.UserId)
                .ToListAsync();

            await SendToUsersAsync(participantIds, senderUserId, new PushMessage(
                Title: string.IsNullOrWhiteSpace(senderName) ? "Mensaje directo" : senderName,
                Body: Trim(preview),
                Data: new Dictionary<string, string> { ["type"] = "dm", ["conversationId"] = conversationId.ToString() }));
        });
    }

    public async Task NotifyComplaintResponseAsync(int complaintId)
    {
        await SafeAsync(async () =>
        {
            using var db = dbFactory.CreateDbContext();
            var complaint = await db.Complaints
                .Select(c => new { c.Id, c.ClientUserId, c.Subject })
                .FirstOrDefaultAsync(c => c.Id == complaintId);
            if (complaint is null) return;

            // Solo al cliente dueño; sin remitente que excluir (lo dispara la oficina)
            await SendToUsersAsync([complaint.ClientUserId], excludeUserId: null, new PushMessage(
                Title: "Atención al Cliente",
                Body: $"La oficina respondió a tu reclamación: {Trim(complaint.Subject)}",
                Data: new Dictionary<string, string> { ["type"] = "complaint", ["complaintId"] = complaintId.ToString() }));
        });
    }

    public async Task NotifyOrderStatusAsync(int orderId, string? performerUserId, OrderStatus newStatus)
    {
        await SafeAsync(async () =>
        {
            using var db = dbFactory.CreateDbContext();
            var order = await db.Orders
                .Select(o => new { o.Id, o.AuthorId, o.AssignedDriverId })
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) return;

            var recipients = new[] { order.AuthorId, order.AssignedDriverId };
            await SendToUsersAsync(recipients, performerUserId, new PushMessage(
                Title: $"Orden #{orderId}",
                Body: $"Estado actualizado: {StatusLabel(newStatus)}",
                Data: new Dictionary<string, string> { ["type"] = "order-status", ["orderId"] = orderId.ToString() }));
        });
    }

    // --- interno ---

    // Resuelve tokens de los destinatarios (excluyendo al emisor), envía y borra
    // los tokens que FCM declaró muertos.
    private async Task SendToUsersAsync(IEnumerable<string?> userIds, string? excludeUserId, PushMessage message)
    {
        if (!push.IsConfigured) return;

        var targets = userIds
            .Where(id => !string.IsNullOrEmpty(id) && id != excludeUserId)
            .Select(id => id!)
            .Distinct()
            .ToList();
        if (targets.Count == 0) return;

        using var db = dbFactory.CreateDbContext();
        var tokens = await db.DeviceTokens
            .Where(t => targets.Contains(t.UserId))
            .Select(t => t.Token)
            .ToListAsync();
        if (tokens.Count == 0) return;

        var dead = await push.SendAsync(tokens, message);
        if (dead.Count > 0)
        {
            await db.DeviceTokens.Where(t => dead.Contains(t.Token)).ExecuteDeleteAsync();
            log.LogInformation("Push: purgados {Count} tokens muertos.", dead.Count);
        }
    }

    private async Task SafeAsync(Func<Task> body)
    {
        try { await body(); }
        catch (Exception ex) { log.LogError(ex, "Push: fallo al notificar (ignorado, best-effort)."); }
    }

    private static string Trim(string s) => s.Length <= 140 ? s : s[..140] + "…";

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
