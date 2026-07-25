using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

// Genera el PAQUETE de conformidad para la ceremonia de firma SIN conexión:
// página de conformidad (datos + declaración + firma + atribución + MANIFIESTO
// del papeleo con sus hashes) seguida del papeleo completo (imágenes y PDFs).
// SECUENCIA CRÍTICA: el paquete se ensambla COMPLETO y es ESO lo que el cliente
// firma — jamás se añade nada después de firmar, así el artefacto firmado y su
// hash siguen describiendo exactamente el archivo que conservamos.
public static class SignaturePdfGenerator
{
    public record PackageAttachment(string Label, PaperworkDocument Doc, byte[] Content);

    public static byte[] Generate(Order order, string signerName, byte[] signaturePng,
        double? latitude, double? longitude, DateTime? capturedAtUtc, DateTime receivedAtUtc,
        string contentHash, IReadOnlyList<PackageAttachment>? paperwork = null)
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);

        var title = new XFont("Arial", 18, XFontStyleEx.Bold);
        var heading = new XFont("Arial", 11, XFontStyleEx.Bold);
        var body = new XFont("Arial", 10, XFontStyleEx.Regular);
        var small = new XFont("Arial", 8, XFontStyleEx.Regular);

        double left = 50, y = 50, width = page.Width.Point - 100;

        gfx.DrawString("Transporte Caribe — Conformidad de Servicio", title, XBrushes.Black,
            new XRect(left, y, width, 26), XStringFormats.TopLeft);
        y += 34;

        // Marca visible de ceremonia offline: la oficina debe saberlo al revisar
        gfx.DrawString("DOCUMENTO PROVISIONAL — firmado en el dispositivo sin conexión",
            heading, XBrushes.DarkRed, new XRect(left, y, width, 16), XStringFormats.TopLeft);
        y += 26;

        void Row(string label, string value)
        {
            gfx.DrawString(label, heading, XBrushes.Black, new XPoint(left, y + 10));
            gfx.DrawString(value, body, XBrushes.Black, new XPoint(left + 150, y + 10));
            y += 18;
        }

        Row("Orden:", $"#{order.Id} — {order.Title}");
        Row("Cliente:", order.Author?.DisplayName ?? order.Author?.Email ?? "—");
        Row("Conductor:", order.AssignedDriver?.DisplayName ?? order.AssignedDriver?.Email ?? "—");
        Row("Tipo:", order.Category?.Name ?? "—");
        Row("Fecha del servicio:", receivedAtUtc.ToString("dd/MM/yyyy"));
        y += 8;

        gfx.DrawString("Descripción del servicio:", heading, XBrushes.Black, new XPoint(left, y + 10));
        y += 18;
        var description = order.Description.Length > 600 ? order.Description[..600] + "…" : order.Description;
        var descRect = new XRect(left, y, width, 90);
        var tf = new XTextFormatter(gfx);
        tf.DrawString(description, body, XBrushes.Black, descRect, XStringFormats.TopLeft);
        y += 100;

        tf.DrawString(
            "El cliente declara que el servicio de mudanza descrito fue realizado y que los bienes " +
            "fueron entregados en las condiciones acordadas, salvo las observaciones registradas en " +
            "el chat de la orden.",
            body, XBrushes.Black, new XRect(left, y, width, 50), XStringFormats.TopLeft);
        y += 60;

        // Firma
        gfx.DrawString("Firma del cliente:", heading, XBrushes.Black, new XPoint(left, y + 10));
        y += 16;
        using (var sigStream = new MemoryStream(signaturePng))
        using (var sigImage = XImage.FromStream(sigStream))
        {
            double sigWidth = 220;
            double sigHeight = sigWidth * sigImage.PixelHeight / sigImage.PixelWidth;
            if (sigHeight > 110) { sigHeight = 110; sigWidth = sigHeight * sigImage.PixelWidth / sigImage.PixelHeight; }
            gfx.DrawRectangle(XPens.Gray, left, y, sigWidth + 10, sigHeight + 10);
            gfx.DrawImage(sigImage, left + 5, y + 5, sigWidth, sigHeight);
            y += sigHeight + 20;
        }
        Row("Nombre del firmante:", signerName);
        y += 10;

        // Metadatos de atribución: es lo que da valor probatorio a la ceremonia offline
        gfx.DrawString("Registro de atribución", heading, XBrushes.Black, new XPoint(left, y + 10));
        y += 18;
        void Meta(string text)
        {
            gfx.DrawString(text, small, XBrushes.DarkSlateGray, new XPoint(left, y + 9));
            y += 13;
        }
        if (capturedAtUtc is not null)
            Meta($"Firmado en el dispositivo (hora local del aparato, no verificada): {capturedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        Meta($"Recibido y sellado por el servidor: {receivedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        Meta(latitude is not null
            ? $"Ubicación GPS en la firma: {latitude}, {longitude}"
            : "Ubicación GPS: no disponible en el momento de la firma");
        Meta($"Huella SHA-256 del contenido firmado (orden + papeleo + firma): {contentHash}");
        Meta("Documento generado automáticamente por Transporte Caribe al recibir la firma.");

        // Manifiesto del papeleo: ata cada documento adjunto (por hash) a ESTA firma
        if (paperwork is { Count: > 0 })
        {
            y += 6;
            gfx.DrawString("Documentación incluida en este paquete firmado:", heading, XBrushes.Black, new XPoint(left, y + 10));
            y += 18;
            foreach (var item in paperwork)
            {
                Meta($"— {item.Label}: SHA-256 {item.Doc.ContentHash}");
            }
        }

        // Páginas del papeleo, en el orden de los slots
        if (paperwork is not null)
        {
            foreach (var item in paperwork)
            {
                if (item.Doc.IsPdf)
                {
                    AppendPdf(document, item, heading, small);
                }
                else
                {
                    AppendImagePage(document, item, heading, small);
                }
            }
        }

        using var output = new MemoryStream();
        document.Save(output);
        return output.ToArray();
    }

    private static void AppendImagePage(PdfDocument document, PackageAttachment item, XFont heading, XFont small)
    {
        var page = document.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);
        double left = 50, top = 50, width = page.Width.Point - 100;

        gfx.DrawString(item.Label, heading, XBrushes.Black, new XPoint(left, top + 10));
        gfx.DrawString($"SHA-256: {item.Doc.ContentHash}", small, XBrushes.DarkSlateGray, new XPoint(left, top + 26));

        using var stream = new MemoryStream(item.Content);
        using var image = XImage.FromStream(stream);
        double maxW = width, maxH = page.Height.Point - top - 90;
        var scale = Math.Min(maxW / image.PixelWidth, maxH / image.PixelHeight);
        double w = image.PixelWidth * scale, h = image.PixelHeight * scale;
        gfx.DrawImage(image, left, top + 40, w, h);
    }

    private static void AppendPdf(PdfDocument document, PackageAttachment item, XFont heading, XFont small)
    {
        // Portada del adjunto + páginas importadas del PDF original
        var cover = document.AddPage();
        using (var gfx = XGraphics.FromPdfPage(cover))
        {
            gfx.DrawString(item.Label, heading, XBrushes.Black, new XPoint(50, 60));
            gfx.DrawString($"Documento PDF adjunto — SHA-256: {item.Doc.ContentHash}", small,
                XBrushes.DarkSlateGray, new XPoint(50, 78));
        }
        using var stream = new MemoryStream(item.Content);
        using var imported = PdfSharp.Pdf.IO.PdfReader.Open(stream, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        foreach (var page in imported.Pages)
        {
            document.AddPage(page);
        }
    }
}
