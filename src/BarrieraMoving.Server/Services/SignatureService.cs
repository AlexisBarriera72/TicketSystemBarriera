using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

// Orquesta el ciclo de vida completo de los documentos de firma (híbrido):
// online por proveedor externo, offline por ceremonia en el dispositivo.
// El PDF firmado SIEMPRE acaba espejado en nuestro almacenamiento.
public class SignatureService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IPhotoStorage storage,
    IAppEmailSender email,
    ISignatureProvider provider,
    ILogger<SignatureService> logger) : ISignatureService
{
    // --- Ruta ONLINE ---

    public async Task<(SignatureDocument?, string?, string?)> CreateProviderRequestAsync(
        int orderId, string requestedByUserId, string signerName)
    {
        using var context = dbFactory.CreateDbContext();
        var order = await context.Orders
            .Include(o => o.Author).Include(o => o.AssignedDriver).Include(o => o.Category)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return (null, null, "La orden no existe.");

        var (envelopeId, signingUrl) = await provider.CreateEnvelopeAsync(order, signerName, order.Author?.Email);

        var doc = new SignatureDocument
        {
            OrderId = orderId,
            RequestedByUserId = requestedByUserId,
            Status = SignatureDocStatus.AwaitingSignature,
            IsProvisional = false,
            ProviderEnvelopeId = envelopeId,
            SignerName = signerName,
            SignerEmail = order.Author?.Email,
        };
        context.SignatureDocuments.Add(doc);
        await context.SaveChangesAsync();
        return (doc, signingUrl, null);
    }

    public async Task<bool> HandleEnvelopeCompletedAsync(string envelopeId)
    {
        using var context = dbFactory.CreateDbContext();
        var doc = await context.SignatureDocuments
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.ProviderEnvelopeId == envelopeId);

        // El webhook debe referirse a un sobre que NOSOTROS creamos; si no, se ignora
        if (doc is null)
        {
            logger.LogWarning("Webhook de firma para sobre desconocido {EnvelopeId} — ignorado.", envelopeId);
            return false;
        }
        if (doc.Status != SignatureDocStatus.AwaitingSignature)
        {
            return true; // reentrega del webhook: idempotente, ya procesado
        }

        // Espejar SIEMPRE el PDF firmado en nuestro almacenamiento
        var pdf = await provider.DownloadSignedPdfAsync(envelopeId);
        var name = Guid.NewGuid().ToString("N");
        doc.PdfPath = await storage.SaveAsync(doc.OrderId, $"doc_{name}.pdf", pdf);
        doc.ContentHash = Sha256Hex(pdf);
        doc.Status = SignatureDocStatus.Signed;
        doc.SignedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();

        await TrySendSignedCopyAsync(context, doc, pdf);
        return true;
    }

    // --- Ruta OFFLINE (ceremonia en el dispositivo) ---

    public async Task<(SignatureDocument?, string?)> CreateOfflineSignedAsync(
        int orderId, string requestedByUserId, string signerName, byte[] signaturePng,
        double? latitude, double? longitude, DateTime? capturedAtUtc, string? idempotencyKey)
    {
        using var context = dbFactory.CreateDbContext();

        // Reintento de la cola del móvil que ya llegó: devolver el original
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await context.SignatureDocuments
                .FirstOrDefaultAsync(d => d.IdempotencyKey == idempotencyKey);
            if (existing is not null) return (existing, null);
        }

        var order = await context.Orders
            .Include(o => o.Author).Include(o => o.AssignedDriver).Include(o => o.Category)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return (null, "La orden no existe.");

        var receivedAt = DateTime.UtcNow;

        // Huella del contenido firmado (lo que el cliente aceptó): orden + firma
        var inputHash = Sha256Hex(Encoding.UTF8.GetBytes(
            $"{order.Id}|{order.Title}|{order.Description}|{signerName}|")
            .Concat(signaturePng).ToArray());

        var pdf = SignaturePdfGenerator.Generate(order, signerName, signaturePng,
            latitude, longitude, capturedAtUtc, receivedAt, inputHash);

        var name = Guid.NewGuid().ToString("N");
        var doc = new SignatureDocument
        {
            OrderId = orderId,
            RequestedByUserId = requestedByUserId,
            Status = SignatureDocStatus.Signed,
            IsProvisional = true, // evidencia más débil: la oficina lo ve marcado
            SignerName = signerName,
            SignerEmail = order.Author?.Email,
            Latitude = latitude,
            Longitude = longitude,
            SignedAtUtc = receivedAt,
            SignedCapturedAtUtc = capturedAtUtc,
            PdfPath = await storage.SaveAsync(orderId, $"doc_{name}.pdf", pdf),
            ContentHash = Sha256Hex(pdf),
            IdempotencyKey = string.IsNullOrEmpty(idempotencyKey) ? null : idempotencyKey,
        };
        context.SignatureDocuments.Add(doc);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException) when (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await context.SignatureDocuments
                .FirstOrDefaultAsync(d => d.IdempotencyKey == idempotencyKey);
            if (existing is not null) return (existing, null);
            throw;
        }

        await TrySendSignedCopyAsync(context, doc, pdf);
        return (doc, null);
    }

    // --- Consultas ---

    public async Task<List<SignatureDocument>> GetForOrderAsync(int orderId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.SignatureDocuments
            .Include(d => d.ReviewedBy)
            .Where(d => d.OrderId == orderId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<SignatureDocument?> GetWithOrderAsync(int documentId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.SignatureDocuments
            .Include(d => d.Order)
            .Include(d => d.ReviewedBy)
            .FirstOrDefaultAsync(d => d.Id == documentId);
    }

    public async Task<List<SignatureDocument>> GetPendingReviewAsync()
    {
        using var context = dbFactory.CreateDbContext();
        return await context.SignatureDocuments
            .Include(d => d.Order)
            .Where(d => d.Status == SignatureDocStatus.Signed)
            .OrderBy(d => d.SignedAtUtc)
            .ToListAsync();
    }

    // --- Revisión de oficina ---

    public async Task<(bool, string?)> ApproveAsync(int documentId, string reviewerUserId)
    {
        using var context = dbFactory.CreateDbContext();
        var doc = await context.SignatureDocuments.FindAsync(documentId);
        if (doc is null) return (false, "El documento no existe.");
        if (doc.Status != SignatureDocStatus.Signed)
            return (false, "Solo se puede aprobar un documento firmado y sin revisar.");

        doc.Status = SignatureDocStatus.Approved;
        doc.ReviewedByUserId = reviewerUserId;
        doc.ReviewedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool, string?)> RejectAsync(int documentId, string reviewerUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "El rechazo necesita un motivo: el conductor debe saber qué rehacer.");

        using var context = dbFactory.CreateDbContext();
        var doc = await context.SignatureDocuments.FindAsync(documentId);
        if (doc is null) return (false, "El documento no existe.");
        if (doc.Status != SignatureDocStatus.Signed)
            return (false, "Solo se puede rechazar un documento firmado y sin revisar.");

        doc.Status = SignatureDocStatus.Rejected;
        doc.ReviewedByUserId = reviewerUserId;
        doc.ReviewedAtUtc = DateTime.UtcNow;
        doc.RejectReason = reason.Trim();
        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool, string?)> ResendEmailAsync(int documentId)
    {
        using var context = dbFactory.CreateDbContext();
        var doc = await context.SignatureDocuments.FindAsync(documentId);
        if (doc is null) return (false, "El documento no existe.");
        if (doc.PdfPath is null) return (false, "El documento aún no tiene PDF firmado.");

        using var stream = storage.Open(doc.PdfPath);
        if (stream is null) return (false, "No se encontró el PDF en el almacenamiento.");
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        await TrySendSignedCopyAsync(context, doc, ms.ToArray());
        return doc.EmailStatus == EmailDeliveryStatus.Sent
            ? (true, null)
            : (false, doc.EmailError ?? "No se pudo enviar el correo.");
    }

    // Enviar la copia firmada al cliente. El fallo es VISIBLE (EmailStatus/EmailError),
    // jamás silencioso: un documento legal sin entregar es un error operativo.
    private async Task TrySendSignedCopyAsync(ApplicationDbContext context, SignatureDocument doc, byte[] pdf)
    {
        if (string.IsNullOrEmpty(doc.SignerEmail))
        {
            doc.EmailStatus = EmailDeliveryStatus.Failed;
            doc.EmailError = "El cliente no tiene email registrado.";
        }
        else if (!email.IsConfigured)
        {
            doc.EmailStatus = EmailDeliveryStatus.NotConfigured;
            doc.EmailError = "Servicio de correo no configurado en el servidor.";
        }
        else
        {
            try
            {
                await email.SendAsync(doc.SignerEmail,
                    $"Copia de tu documento firmado — Orden #{doc.OrderId}",
                    "Adjuntamos la copia del documento de conformidad que firmaste " +
                    "para tu mudanza con Barriera Moving. Consérvala para tus registros.\n\n" +
                    "— Barriera Moving",
                    pdf, $"Conformidad_Orden_{doc.OrderId}.pdf");
                doc.EmailStatus = EmailDeliveryStatus.Sent;
                doc.EmailError = null;
            }
            catch (Exception ex)
            {
                doc.EmailStatus = EmailDeliveryStatus.Failed;
                doc.EmailError = ex.Message;
                logger.LogError(ex, "Fallo enviando la copia firmada del documento {DocId}", doc.Id);
            }
        }
        await context.SaveChangesAsync();
    }

    private static string Sha256Hex(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
