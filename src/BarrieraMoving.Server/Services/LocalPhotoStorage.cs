namespace BarrieraMoving.Server.Services;

// Disco local, FUERA de wwwroot: las fotos jamás se sirven como archivos estáticos,
// solo a través del endpoint que revalida el ACL de la orden en cada petición.
public class LocalPhotoStorage : IPhotoStorage
{
    private readonly string _root;

    public LocalPhotoStorage(IConfiguration config, IWebHostEnvironment env)
    {
        _root = config["Photos:RootPath"]
            ?? Path.Combine(env.ContentRootPath, "App_Data", "photos");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(int orderId, string fileName, byte[] content)
    {
        var relative = Path.Combine(orderId.ToString(), fileName);
        var full = SafeFullPath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, content);
        return relative.Replace('\\', '/');
    }

    public Stream? Open(string relativeKey)
    {
        var full = SafeFullPath(relativeKey);
        return File.Exists(full) ? File.OpenRead(full) : null;
    }

    // Las claves las genera siempre el servidor (GUIDs), pero cinturón y tirantes:
    // nada fuera de la raíz de fotos.
    private string SafeFullPath(string relativeKey)
    {
        // El prefijo se compara CON separador final: si no, una raíz "/fotos"
        // aceptaría por error una ruta "/fotos-otra/x".
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_root))
                   + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(_root, relativeKey));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Clave de foto fuera de la raíz de almacenamiento.");
        }
        return full;
    }
}
