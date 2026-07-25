namespace BarrieraMoving.Server.Models;

// Token de registro FCM de UN dispositivo, ligado al usuario que inició sesión en él.
// El token es global-único (un dispositivo = un token); si el mismo dispositivo cambia
// de usuario, la fila se re-asigna. Nunca es un secreto del servidor (lo emite Google
// al dispositivo), pero identifica a un aparato concreto → se trata como dato personal.
public class DeviceToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Token { get; set; } = default!;      // registro FCM (o APNs en el futuro)
    public string Platform { get; set; } = "android";  // "android" | "ios"
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}
