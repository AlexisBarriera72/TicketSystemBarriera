using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

// Proveedor externo de firma con pista de auditoría (ruta ONLINE del híbrido).
// El adaptador real (SignWell/Dropbox Sign/BoldSign…) es UNA clase que implementa
// esto cuando Alexis nombre el proveedor y ponga ESign:ApiKey en user-secrets.
public interface ISignatureProvider
{
    string Name { get; }
    bool IsRealProvider { get; }

    // Crea el sobre de firma embebida y devuelve (envelopeId, url de firma para
    // abrir en el dispositivo del conductor con el cliente delante)
    Task<(string EnvelopeId, string SigningUrl)> CreateEnvelopeAsync(Order order, string signerName, string? signerEmail);

    // Descarga el PDF firmado final (con certificado de auditoría) para espejarlo
    // en NUESTRO almacenamiento — los documentos son nuestros aunque se deje de
    // pagar al proveedor
    Task<byte[]> DownloadSignedPdfAsync(string envelopeId);
}

// Proveedor FALSO para desarrollo: permite ejercitar el flujo completo
// (sobre → webhook → espejo → email) sin cuenta externa. Queda claramente
// marcado en logs y en el PDF que genera.
public class FakeSignatureProvider(ILogger<FakeSignatureProvider> logger) : ISignatureProvider
{
    public string Name => "FAKE (desarrollo)";
    public bool IsRealProvider => false;

    public Task<(string, string)> CreateEnvelopeAsync(Order order, string signerName, string? signerEmail)
    {
        var envelopeId = $"fake-{Guid.NewGuid():N}";
        logger.LogWarning("Proveedor de firma FAKE: sobre {EnvelopeId} para la orden {OrderId}. " +
            "Configura el proveedor real (ESign:ApiKey) antes de producción.", envelopeId, order.Id);
        return Task.FromResult((envelopeId, $"about:blank#fake-signing/{envelopeId}"));
    }

    public Task<byte[]> DownloadSignedPdfAsync(string envelopeId)
    {
        // PDF mínimo de prueba, inconfundible con un documento real
        using var doc = new PdfSharp.Pdf.PdfDocument();
        var page = doc.AddPage();
        using var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
        gfx.DrawString($"DOCUMENTO DE PRUEBA — proveedor FAKE — sobre {envelopeId}",
            new PdfSharp.Drawing.XFont("Arial", 12), PdfSharp.Drawing.XBrushes.Red,
            new PdfSharp.Drawing.XPoint(40, 60));
        using var ms = new MemoryStream();
        doc.Save(ms);
        return Task.FromResult(ms.ToArray());
    }
}
