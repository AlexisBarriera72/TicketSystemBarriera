using SkiaSharp;

namespace BarrieraMoving.Server.Services;

// Re-codifica TODA imagen subida. Esto cumple tres funciones a la vez:
// 1. Validación real (si Skia no puede decodificarla, no es una imagen — da igual
//    la extensión o el Content-Type que declare el cliente).
// 2. EXIF fuera POR CONSTRUCCIÓN: decodificar píxeles y re-codificar JPEG nunca
//    copia metadatos (GPS de la cámara, número de serie, etc.). Las fotos de la
//    casa de un cliente son sensibles; solo viaja el GPS que capturamos adrede.
// 3. Tamaño acotado en el servidor aunque un cliente no comprimiera.
public static class PhotoProcessor
{
    public const int MaxEdge = 1600;
    public const int ThumbEdge = 200;
    public const int JpegQuality = 75;

    // null = el contenido no es una imagen decodificable
    public static (byte[] Jpeg, byte[] Thumb)? ReencodeAsJpeg(Stream input)
    {
        using var original = SKBitmap.Decode(input);
        if (original is null) return null;

        var jpeg = EncodeResized(original, MaxEdge);
        var thumb = EncodeResized(original, ThumbEdge);
        return (jpeg, thumb);
    }

    private static byte[] EncodeResized(SKBitmap source, int maxEdge)
    {
        SKBitmap bitmap = source;
        var longest = Math.Max(source.Width, source.Height);
        if (longest > maxEdge)
        {
            var scale = (float)maxEdge / longest;
            var info = new SKImageInfo(
                Math.Max(1, (int)(source.Width * scale)),
                Math.Max(1, (int)(source.Height * scale)));
            bitmap = source.Resize(info, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
                ?? source;
        }

        try
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
            return data.ToArray();
        }
        finally
        {
            if (!ReferenceEquals(bitmap, source)) bitmap.Dispose();
        }
    }
}
