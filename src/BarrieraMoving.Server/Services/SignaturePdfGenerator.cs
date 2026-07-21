using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

// Genera el PDF de conformidad para la ceremonia de firma SIN conexión:
// datos de la orden + declaración + imagen de la firma + metadatos de atribución
// (nombre tecleado, GPS, hora del dispositivo, hora del servidor, hash).
// En la ruta online el PDF lo genera el PROVEEDOR; este documento se marca
// PROVISIONAL de forma bien visible para que la oficina lo sepa al revisar.
public static class SignaturePdfGenerator
{
    public static byte[] Generate(Order order, string signerName, byte[] signaturePng,
        double? latitude, double? longitude, DateTime? capturedAtUtc, DateTime receivedAtUtc,
        string contentHash)
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);

        var title = new XFont("Arial", 18, XFontStyleEx.Bold);
        var heading = new XFont("Arial", 11, XFontStyleEx.Bold);
        var body = new XFont("Arial", 10, XFontStyleEx.Regular);
        var small = new XFont("Arial", 8, XFontStyleEx.Regular);

        double left = 50, y = 50, width = page.Width.Point - 100;

        gfx.DrawString("Barriera Moving — Conformidad de Servicio", title, XBrushes.Black,
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
        Meta($"Huella SHA-256 del contenido firmado (orden + firma): {contentHash}");
        Meta("Documento generado automáticamente por Barriera Moving al recibir la firma.");

        using var output = new MemoryStream();
        document.Save(output);
        return output.ToArray();
    }
}
