using Microsoft.AspNetCore.Identity;

namespace BarrieraMoving.Server.Data;

public class ApplicationUser : IdentityUser
{
    // Nombre visible en el chat y en la app (ej. "Luis (Conductor)")
    public string? DisplayName { get; set; }
}
