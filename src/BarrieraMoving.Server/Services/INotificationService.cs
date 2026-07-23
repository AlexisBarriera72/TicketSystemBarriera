using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

// Capa de ALTO NIVEL: sabe QUIÉN debe recibir cada evento y traduce a tokens.
// Todos los métodos son best-effort: tragan sus propios errores y NUNCA lanzan,
// para no romper el envío del mensaje/cambio de estado que los disparó.
public interface INotificationService
{
    Task NotifyOrderMessageAsync(int orderId, string senderUserId, string senderName, string preview);
    Task NotifyDirectMessageAsync(int conversationId, string senderUserId, string senderName, string preview);
    Task NotifyComplaintResponseAsync(int complaintId);
    Task NotifyOrderStatusAsync(int orderId, string? performerUserId, OrderStatus newStatus);
}
