using System.Security.Claims;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared;

namespace BarrieraMoving.Server.Api;

// Feed de notificaciones para el móvil. El "no leído" lo calcula el cliente con su
// marca local de "visto por última vez" — el servidor solo entrega el feed derivado.
public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationApi(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Notifications, async (ClaimsPrincipal user, INotificationFeedService feed) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isStaff = user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Office);
            return Results.Ok(await feed.GetFeedAsync(userId, isStaff));
        })
        .WithTags("Notifications")
        .RequireAuthorization(ApiAuth.Policy);

        return app;
    }
}
