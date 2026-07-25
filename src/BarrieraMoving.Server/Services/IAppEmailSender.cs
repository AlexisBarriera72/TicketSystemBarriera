namespace BarrieraMoving.Server.Services;

// Correo saliente de la app (copias firmadas al cliente). Distinto del
// IEmailSender de Identity (confirmaciones de cuenta), que sigue siendo no-op.
public interface IAppEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string to, string subject, string textBody,
        byte[]? attachment = null, string? attachmentName = null);
}

// Sin servicio de correo configurado: el estado queda VISIBLE como
// NotConfigured — un documento legal sin enviar nunca debe ser silencioso.
public class NullEmailSender : IAppEmailSender
{
    public bool IsConfigured => false;
    public Task SendAsync(string to, string subject, string textBody,
        byte[]? attachment = null, string? attachmentName = null) =>
        throw new InvalidOperationException("No hay servicio de correo configurado (Email:Host en user-secrets).");
}
