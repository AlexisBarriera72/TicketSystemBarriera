namespace BarrieraMoving.Server.Models;

// Foto que un visitante ANÓNIMO adjunta a su solicitud de cotización.
//
// Mismas reglas que las fotos de una orden (fase 5), y por los mismos motivos:
//   - se re-codifica en el servidor con SkiaSharp, así que los EXIF (incluida la
//     geolocalización de la casa del cliente) desaparecen por construcción;
//   - se guarda FUERA de wwwroot y solo la sirve un endpoint que exige rol de
//     oficina o admin: una foto del interior de una vivienda no puede quedar en
//     una URL adivinable.
public class QuotePhoto
{
    public int Id { get; set; }

    public int QuoteRequestId { get; set; }
    public QuoteRequest? QuoteRequest { get; set; }

    // Claves relativas dentro del almacén de fotos (nunca rutas absolutas)
    public required string FilePath { get; set; }
    public required string ThumbPath { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
