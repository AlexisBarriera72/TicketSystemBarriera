using System.Security.Claims;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Api;

// Reglas de acceso por orden, compartidas por los endpoints de órdenes y de fotos
internal static class OrderAccess
{
    // Forbid con el esquema JWT explícito: el Forbid por defecto usa la cookie de
    // Identity y responde con un 302 al login web — la API debe devolver 403 limpio
    internal static IResult ApiForbid() =>
        Results.Forbid(authenticationSchemes: [Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme]);

    // Admin/Oficina ven todo; el conductor asignado y el autor ven su orden
    internal static bool CanAccess(ClaimsPrincipal user, Order order)
    {
        if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Office)) return true;

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.IsInRole(Roles.Driver) && order.AssignedDriverId == userId) return true;
        return order.AuthorId == userId;
    }
}
