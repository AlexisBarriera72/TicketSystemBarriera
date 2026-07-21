using System.Security.Claims;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;
using static BarrieraMoving.Server.Api.OrderAccess;

namespace BarrieraMoving.Server.Api;

// Mensajes directos 1:1. REGLA DE ACCESO: pertenencia al conjunto de
// participantes — cero lógica de órdenes. Un cliente jamás alcanza una
// conversación en la que el personal no lo haya incluido explícitamente.
public static class DirectMessageEndpoints
{
    public static IEndpointRouteBuilder MapDirectMessageApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.DirectMessages)
            .WithTags("DirectMessages")
            .RequireAuthorization(ApiAuth.Policy);

        // Mis conversaciones (con participantes y último mensaje)
        group.MapGet("/", async (ClaimsPrincipal user, IDirectMessageService dm) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var conversations = await dm.GetMineAsync(userId);
            var result = new List<DirectConversationDto>();
            foreach (var conv in conversations)
            {
                var last = await dm.GetLastMessageAsync(conv.Id);
                result.Add(new DirectConversationDto(
                    conv.Id,
                    conv.Participants.Select(p => p.User!.ToDto()).ToList(),
                    last?.ToDto()));
            }
            return Results.Ok(result.OrderByDescending(c => c.LastMessage?.CreatedAt ?? DateTime.MinValue));
        });

        // Iniciar conversación: SOLO Admin/Oficina (el jefe escribe a quien quiera;
        // empleados y clientes responden en las que ya están)
        group.MapPost("/", async (StartConversationRequest request,
            ClaimsPrincipal user, IDirectMessageService dm) =>
        {
            var (conv, error) = await dm.GetOrCreateAsync(
                user.FindFirstValue(ClaimTypes.NameIdentifier)!, request.OtherUserId);
            if (conv is null) return Results.BadRequest(error);

            var last = await dm.GetLastMessageAsync(conv.Id);
            return Results.Ok(new DirectConversationDto(
                conv.Id,
                conv.Participants.Select(p => p.User!.ToDto()).ToList(),
                last?.ToDto()));
        }).RequireAuthorization(ApiAuth.StaffPolicy);

        group.MapGet("/{id:int}/messages", async (int id, int? take, int? beforeId, int? afterId,
            ClaimsPrincipal user, IDirectMessageService dm) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await dm.IsParticipantAsync(id, userId)) return ApiForbid();

            var messages = await dm.GetMessagesAsync(id, take ?? 50, beforeId, afterId);
            return Results.Ok(messages.Select(m => m.ToDto()));
        });

        group.MapPost("/{id:int}/messages", async (int id, SendDirectMessageRequest request,
            ClaimsPrincipal user, IDirectMessageService dm) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await dm.IsParticipantAsync(id, userId)) return ApiForbid();

            var (message, error) = await dm.SendAsync(id, userId, Roles.PrimaryRole(user),
                request.Content, request.CapturedAtUtc, request.IdempotencyKey);
            return message is null
                ? Results.BadRequest(error)
                : Results.Created($"{ApiRoutes.DirectMessages}/{id}/messages", message.ToDto());
        });

        return app;
    }
}
