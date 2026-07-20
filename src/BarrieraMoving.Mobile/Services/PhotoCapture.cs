using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace BarrieraMoving.Mobile.Services;

// Captura/selección de foto + compresión ANTES de subir: una foto de móvil de
// 5-12 MB se convierte en un JPEG de lado mayor 1600 px y calidad 75 (~200-400 KB)
// — los conductores van con datos móviles. El servidor la re-codifica igualmente
// (y elimina el EXIF), pero comprimir aquí ahorra el 95 % de la subida.
public static class PhotoCapture
{
    public const int MaxEdge = 1600;
    public const float JpegQuality = 0.75f;

    public sealed record CaptureResult(byte[]? Jpeg, string? Error);

    // Cámara. Permiso denegado → Error explicando que la galería sigue disponible.
    public static async Task<CaptureResult> FromCameraAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                return new(null, "Este dispositivo no tiene cámara disponible.");
            }
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null) return new(null, null); // cancelado por el usuario
            return new(await CompressAsync(photo), null);
        }
        catch (PermissionException)
        {
            // La cámara NUNCA bloquea el trabajo: la galería no requiere permiso
            return new(null, "Sin permiso de cámara. Puedes usar la galería (🖼).");
        }
        catch (Exception)
        {
            return new(null, "No se pudo capturar la foto.");
        }
    }

    // Galería (Photo Picker del sistema: no necesita permiso en Android moderno)
    public static async Task<CaptureResult> FromGalleryAsync()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo is null) return new(null, null); // cancelado
            return new(await CompressAsync(photo), null);
        }
        catch (Exception)
        {
            return new(null, "No se pudo abrir la galería.");
        }
    }

    private static async Task<byte[]> CompressAsync(FileResult photo)
    {
        await using var input = await photo.OpenReadAsync();
        IImage image = PlatformImage.FromStream(input);
        if (Math.Max(image.Width, image.Height) > MaxEdge)
        {
            image = image.Downsize(MaxEdge, disposeOriginal: true);
        }
        using var output = new MemoryStream();
        await image.SaveAsync(output, Microsoft.Maui.Graphics.ImageFormat.Jpeg, JpegQuality);
        image.Dispose();
        return output.ToArray();
    }
}
