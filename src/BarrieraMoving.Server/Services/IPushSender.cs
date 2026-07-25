namespace BarrieraMoving.Server.Services;

// Contenido de una notificación push. Data = pares clave/valor que la app usa
// para navegar al pulsar (p.ej. {"type":"chat","orderId":"42"}).
public record PushMessage(string Title, string Body, IReadOnlyDictionary<string, string>? Data = null);

// Envío push de BAJO NIVEL. No sabe de órdenes ni usuarios: recibe tokens ya
// resueltos. Devuelve los tokens que el proveedor marcó como MUERTOS (dispositivo
// desinstalado / token caducado) para que la capa superior los borre de la BD.
// APNs (iOS) implementará esta misma interfaz en el futuro sin tocar a los que llaman.
public interface IPushSender
{
    bool IsConfigured { get; }

    Task<IReadOnlyCollection<string>> SendAsync(
        IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken ct = default);
}

// Estado NotConfigured VISIBLE: si no hay credencial de Firebase, el envío no
// falla en silencio ni rompe el hilo que lo llamó — simplemente no hace nada.
public sealed class NullPushSender(ILogger<NullPushSender> log) : IPushSender
{
    public bool IsConfigured => false;

    public Task<IReadOnlyCollection<string>> SendAsync(
        IReadOnlyCollection<string> tokens, PushMessage message, CancellationToken ct = default)
    {
        log.LogDebug("Push NotConfigured: se omitió el envío de \"{Title}\" a {Count} dispositivos.",
            message.Title, tokens.Count);
        return Task.FromResult<IReadOnlyCollection<string>>([]);
    }
}
