namespace BarrieraMoving.Mobile.Services;

// Puente entre la notificación push (mundo Android) y el router de Blazor.
// La notificación puede llegar ANTES de que la WebView exista, así que la ruta
// se guarda y la consume MainLayout cuando ya puede navegar.
public class DeepLinkState
{
    private string? _pending;
    public event Action? Changed;

    // Traduce los datos que manda el servidor (ver NotificationService) a una ruta.
    public void SetFromPush(IDictionary<string, string?> data)
    {
        var type = Get(data, "type");
        var route = type switch
        {
            "order-chat" when Get(data, "orderId") is { } o => $"/orders/{o}/chat",
            "order-status" when Get(data, "orderId") is { } o => $"/orders/{o}",
            "dm" when Get(data, "conversationId") is { } c => $"/dm/{c}",
            "complaint" => "/complaints",
            _ => null,
        };
        if (route is null) return;

        _pending = route;
        Changed?.Invoke();
    }

    // Devuelve la ruta pendiente UNA sola vez (no repetir la navegación).
    public string? Consume()
    {
        var r = _pending;
        _pending = null;
        return r;
    }

    private static string? Get(IDictionary<string, string?> d, string key) =>
        d.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;
}
