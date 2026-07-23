using System.Security.Claims;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;
using static BarrieraMoving.Server.Api.OrderAccess;

namespace BarrieraMoving.Server.Api;

// Atención al cliente / reclamaciones. ACL: el cliente ve y crea SOLO las suyas;
// el personal (Admin/Oficina) ve todas y responde.
public static class ComplaintEndpoints
{
    public static IEndpointRouteBuilder MapComplaintApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Complaints)
            .WithTags("Complaints")
            .RequireAuthorization(ApiAuth.Policy);

        // Crear una reclamación (cualquier autenticado; normalmente el cliente)
        group.MapPost("/", async (CreateComplaintRequest request, ClaimsPrincipal user, IComplaintService complaints) =>
        {
            if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Description))
                return Results.BadRequest("Asunto y descripción son obligatorios.");

            var complaint = await complaints.CreateAsync(
                user.FindFirstValue(ClaimTypes.NameIdentifier)!,
                request.Subject, request.Description, request.OrderId);
            return Results.Created($"{ApiRoutes.Complaints}/{complaint.Id}", complaint.ToDto());
        });

        // Mis reclamaciones (las del usuario actual)
        group.MapGet("/mine", async (ClaimsPrincipal user, IComplaintService complaints) =>
        {
            var mine = await complaints.GetForClientAsync(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Results.Ok(mine.Select(c => c.ToDto()));
        });

        // Todas (solo Admin/Oficina)
        group.MapGet("/", async (IComplaintService complaints) =>
            Results.Ok((await complaints.GetAllAsync()).Select(c => c.ToDto())))
            .RequireAuthorization(ApiAuth.StaffPolicy);

        // Detalle: el dueño o el personal
        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, IComplaintService complaints) =>
        {
            var complaint = await complaints.GetByIdAsync(id);
            if (complaint is null) return Results.NotFound();

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isStaff = user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Office);
            if (!isStaff && complaint.ClientUserId != userId) return ApiForbid();

            return Results.Ok(complaint.ToDto());
        });

        // Responder / resolver (solo Admin/Oficina)
        group.MapPost("/{id:int}/respond", async (int id, RespondComplaintRequest request,
            ClaimsPrincipal user, IComplaintService complaints, INotificationService notify) =>
        {
            var (ok, error) = await complaints.RespondAsync(id,
                user.FindFirstValue(ClaimTypes.NameIdentifier)!, request.Response, request.Resolve);
            if (!ok) return Results.BadRequest(error);

            await notify.NotifyComplaintResponseAsync(id);
            return Results.NoContent();
        }).RequireAuthorization(ApiAuth.StaffPolicy);

        return app;
    }
}
