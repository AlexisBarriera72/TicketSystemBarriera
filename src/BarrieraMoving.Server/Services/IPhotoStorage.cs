namespace BarrieraMoving.Server.Services;

// Persistencia de fotos detrás de una interfaz: hoy disco local; si el despliegue
// acaba en la nube, un blob storage es UNA clase nueva + config — el esquema no
// cambia porque en la BD solo se guardan claves relativas.
public interface IPhotoStorage
{
    // Guarda el contenido y devuelve la clave relativa (p. ej. "12/abc123.jpg")
    Task<string> SaveAsync(int orderId, string fileName, byte[] content);

    // null si la clave no existe
    Stream? Open(string relativeKey);
}
