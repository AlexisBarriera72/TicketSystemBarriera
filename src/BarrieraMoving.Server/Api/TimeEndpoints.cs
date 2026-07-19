using System.Security.Claims;
using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Server.Api;

// Fichajes. Solo personal (Admin/Oficina/Conductor) — los clientes no fichan.
public static class TimeEndpoints
{
    public static IEndpointRouteBuilder MapTimeApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Time)
            .WithTags("Time")
            .RequireAuthorization(ApiAuth.EmployeePolicy);

        // Jornada abierta del usuario actual (204 si no hay)
        group.MapGet("/current", async (ClaimsPrincipal user, ITimeService time) =>
        {
            var open = await time.GetOpenEntryAsync(UserId(user));
            return open is null ? Results.NoContent() : Results.Ok(open.ToDto());
        });

        group.MapPost("/clock-in", async (ClockRequest request, ClaimsPrincipal user, ITimeService time) =>
        {
            var (entry, error) = await time.ClockInAsync(UserId(user), request.Latitude, request.Longitude);
            return entry is null
                ? Results.Conflict(error)
                : Results.Created($"{ApiRoutes.Time}/current", entry.ToDto());
        });

        group.MapPost("/clock-out", async (ClockRequest request, ClaimsPrincipal user, ITimeService time) =>
        {
            var (entry, error) = await time.ClockOutAsync(UserId(user), request.Latitude, request.Longitude);
            return entry is null ? Results.Conflict(error) : Results.Ok(entry.ToDto());
        });

        // Historial propio
        group.MapGet("/history", async (int? days, ClaimsPrincipal user, ITimeService time) =>
        {
            var entries = await time.GetRecentEntriesAsync(Math.Clamp(days ?? 14, 1, 90), UserId(user));
            return Results.Ok(entries.Select(t => t.ToDto()));
        });

        // Dashboard del jefe: quién está fichado ahora
        group.MapGet("/active", async (ITimeService time) =>
                Results.Ok((await time.GetActiveEntriesAsync()).Select(t => t.ToDto())))
            .RequireAuthorization(ApiAuth.StaffPolicy);

        // Dashboard del jefe: entradas recientes, filtrables por empleado
        group.MapGet("/entries", async (string? userId, int? days, ITimeService time) =>
        {
            var entries = await time.GetRecentEntriesAsync(Math.Clamp(days ?? 7, 1, 90), userId);
            return Results.Ok(entries.Select(t => t.ToDto()));
        }).RequireAuthorization(ApiAuth.StaffPolicy);

        return app;
    }

    private static string UserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
