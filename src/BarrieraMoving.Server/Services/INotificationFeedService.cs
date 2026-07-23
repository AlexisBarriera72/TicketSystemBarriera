using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Server.Services;

// Feed de notificaciones DERIVADO de datos existentes (sin tabla propia): órdenes
// asignadas + mensajes nuevos + DMs. El "no leído" lo marca el cliente con una fecha
// local de "visto por última vez" (móvil: Preferences; web: localStorage).
public interface INotificationFeedService
{
    Task<List<NotificationItemDto>> GetFeedAsync(string userId, bool isStaff, int take = 30);
}
