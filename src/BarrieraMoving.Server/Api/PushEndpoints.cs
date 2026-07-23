using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Server.Api;

// Registro del token FCM del dispositivo. El token es global-único: si ya existe
// (el mismo aparato lo re-envía o cambió de usuario), se re-asigna al usuario
// actual en lugar de duplicarse.
public static class PushEndpoints
{
    public static IEndpointRouteBuilder MapPushApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Push)
            .WithTags("Push")
            .RequireAuthorization(ApiAuth.Policy);

        group.MapPost("/register", async (RegisterPushTokenRequest request,
            ClaimsPrincipal user, IDbContextFactory<ApplicationDbContext> dbFactory) =>
        {
            if (string.IsNullOrWhiteSpace(request.Token)) return Results.BadRequest("Token vacío.");
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            using var db = dbFactory.CreateDbContext();
            var existing = await db.DeviceTokens.FirstOrDefaultAsync(t => t.Token == request.Token);
            if (existing is null)
            {
                db.DeviceTokens.Add(new DeviceToken
                {
                    UserId = userId,
                    Token = request.Token,
                    Platform = string.IsNullOrWhiteSpace(request.Platform) ? "android" : request.Platform,
                });
            }
            else
            {
                existing.UserId = userId; // el aparato pudo cambiar de usuario
                existing.LastSeenUtc = DateTime.UtcNow;
            }

            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                // Carrera con otro registro simultáneo del mismo token: es idempotente
            }
            return Results.NoContent();
        });

        // Al cerrar sesión el dispositivo borra su token para dejar de recibir
        group.MapPost("/unregister", async (UnregisterPushTokenRequest request,
            ClaimsPrincipal user, IDbContextFactory<ApplicationDbContext> dbFactory) =>
        {
            if (string.IsNullOrWhiteSpace(request.Token)) return Results.NoContent();
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

            using var db = dbFactory.CreateDbContext();
            // Solo puede borrar SU propio token (no el de otro aparato/usuario)
            await db.DeviceTokens
                .Where(t => t.Token == request.Token && t.UserId == userId)
                .ExecuteDeleteAsync();
            return Results.NoContent();
        });

        return app;
    }
}
