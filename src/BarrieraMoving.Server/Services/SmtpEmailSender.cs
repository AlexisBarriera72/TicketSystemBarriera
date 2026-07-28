using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BarrieraMoving.Server.Services;

// SMTP vía MailKit. Config (user-secrets, NUNCA en git):
//   Email:Host, Email:Port (587), Email:User, Email:Password, Email:From, Email:FromName
public class SmtpEmailSender(IConfiguration config) : IAppEmailSender
{
    public bool IsConfigured => !string.IsNullOrEmpty(config["Email:Host"]);

    public async Task SendAsync(string to, string subject, string textBody,
        byte[]? attachment = null, string? attachmentName = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            config["Email:FromName"] ?? "Transporte Caribe Logistic",
            config["Email:From"] ?? config["Email:User"]));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder { TextBody = textBody };
        if (attachment is not null)
        {
            builder.Attachments.Add(attachmentName ?? "documento.pdf", attachment,
                new ContentType("application", "pdf"));
        }
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(config["Email:Host"],
            int.TryParse(config["Email:Port"], out var port) ? port : 587,
            SecureSocketOptions.StartTlsWhenAvailable);
        if (!string.IsNullOrEmpty(config["Email:User"]))
        {
            await client.AuthenticateAsync(config["Email:User"], config["Email:Password"]);
        }
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
