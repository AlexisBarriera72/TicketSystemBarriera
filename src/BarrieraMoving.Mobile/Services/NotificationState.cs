using System.Globalization;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Mobile.Services;

// Estado compartido del feed de notificaciones. El "no leído" es LOCAL al dispositivo:
// se compara la fecha de cada item contra una marca "visto por última vez" en
// Preferences (mismo enfoque derivado que el servidor — sin tabla de notificaciones).
public class NotificationState
{
    private const string LastSeenPref = "notif_last_seen";

    public int Unread { get; private set; }
    public List<NotificationItemDto> Items { get; private set; } = [];
    public event Action? Changed;

    public async Task RefreshAsync(ApiClient api)
    {
        try { Items = await api.GetNotificationsAsync(); }
        catch { return; } // sin red: mantenemos lo último; no molestamos al usuario
        var lastSeen = GetLastSeen();
        Unread = Items.Count(i => i.CreatedAtUtc > lastSeen);
        Changed?.Invoke();
    }

    // Al abrir la pantalla de notificaciones se marca todo como visto
    public void MarkAllSeen()
    {
        Preferences.Set(LastSeenPref, DateTime.UtcNow.ToString("O"));
        Unread = 0;
        Changed?.Invoke();
    }

    private static DateTime GetLastSeen()
    {
        var s = Preferences.Get(LastSeenPref, null);
        return DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var dt)
            ? dt.ToUniversalTime()
            : DateTime.MinValue;
    }
}
