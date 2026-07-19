using System.Security.Claims;

namespace BarrieraMoving.Server.Data;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Office = "Office";
    public const string Driver = "Driver";
    public const string Client = "Client";

    // Rol principal de un usuario con varios roles (para etiquetar mensajes)
    public static string? PrimaryRole(ClaimsPrincipal user) =>
        user.IsInRole(Admin) ? Admin :
        user.IsInRole(Office) ? Office :
        user.IsInRole(Driver) ? Driver :
        user.IsInRole(Client) ? Client : null;
}
