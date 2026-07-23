using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;
using static BarrieraMoving.Server.Api.OrderAccess;

namespace BarrieraMoving.Server.Api;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Orders)
            .WithTags("Orders")
            .RequireAuthorization(ApiAuth.Policy);

        // Lista según rol: Admin/Oficina todo, conductor lo suyo, cliente lo que creó
        group.MapGet("/", async (ClaimsPrincipal user, IOrderService orders) =>
            Results.Ok((await orders.GetOrdersForUserAsync(user)).Select(o => o.ToDto())));

        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, IOrderService orders) =>
        {
            var order = await orders.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();
            if (!CanAccess(user, order)) return ApiForbid();
            return Results.Ok(order.ToDto());
        });

        group.MapPost("/", async (CreateOrderRequest request, ClaimsPrincipal user, IOrderService orders) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) ||
                string.IsNullOrWhiteSpace(request.Description) ||
                request.CategoryId <= 0)
            {
                return Results.BadRequest("Title, Description y CategoryId son obligatorios.");
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var order = new Order
            {
                Title = request.Title,
                Description = request.Description,
                CategoryId = request.CategoryId,
                Priority = request.Priority,
                AuthorId = userId,
            };
            await orders.CreateOrderAsync(order);

            var created = await orders.GetOrderByIdAsync(order.Id);
            return Results.Created($"{ApiRoutes.Orders}/{order.Id}", created!.ToDto());
        });

        // Asignar conductor (solo Admin/Oficina)
        group.MapPost("/{id:int}/assign", async (int id, AssignDriverRequest request,
            ClaimsPrincipal user, IOrderService orders,
            Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.DriverId))
                return Results.BadRequest("DriverId es obligatorio.");

            var driver = await userManager.FindByIdAsync(request.DriverId);
            if (driver is null || !await userManager.IsInRoleAsync(driver, Roles.Driver))
                return Results.BadRequest("El usuario indicado no existe o no tiene el rol Driver.");

            var order = await orders.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var (ok, error) = await orders.UpdateOrderStatusAsync(id, order.Status, request.DriverId, userId);
            return ok ? Results.NoContent() : Results.BadRequest(error ?? "No se pudo asignar el conductor.");
        }).RequireAuthorization(ApiAuth.StaffPolicy);

        // Cambio de estado: el conductor asignado sigue el flujo; Admin/Oficina pueden saltárselo
        group.MapPost("/{id:int}/status", async (int id, UpdateStatusRequest request,
            ClaimsPrincipal user, IOrderService orders) =>
        {
            var order = await orders.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isStaff = user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Office);
            var isAssignedDriver = user.IsInRole(Roles.Driver) && order.AssignedDriverId == userId;
            if (!isStaff && !isAssignedDriver) return ApiForbid();

            var (ok, error) = await orders.UpdateOrderStatusAsync(id, request.NewStatus, null, userId, bypassValidation: isStaff);
            return ok ? Results.NoContent() : Results.BadRequest(
                error ?? $"Transición no permitida: {order.Status} → {request.NewStatus}.");
        });

        // Chat de la orden. Paginado: ?take= (máx 200, def. 50) devuelve los últimos;
        // ?beforeId= página hacia atrás; ?afterId= solo nuevos (polling delta).
        group.MapGet("/{id:int}/messages", async (int id, int? take, int? beforeId, int? afterId,
            ClaimsPrincipal user, IOrderService orders) =>
        {
            var order = await orders.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();
            if (!CanAccess(user, order)) return ApiForbid();

            var messages = await orders.GetMessagesAsync(id, take ?? 50, beforeId, afterId);
            return Results.Ok(messages.Select(m => m.ToDto()));
        });

        group.MapPost("/{id:int}/messages", async (int id, CreateMessageRequest request,
            ClaimsPrincipal user, IOrderService orders, INotificationService notify) =>
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return Results.BadRequest("Content es obligatorio.");

            var order = await orders.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();
            if (!CanAccess(user, order)) return ApiForbid();

            // Reenvío de la cola offline: si esta clave ya se insertó, devolver el original
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var existing = await orders.FindMessageByIdempotencyKeyAsync(request.IdempotencyKey);
                if (existing is not null) return Results.Ok(existing.ToDto());
            }

            var message = new Message
            {
                OrderId = id,
                Content = request.Content.Trim(),
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
                CreatedAt = DateTime.UtcNow, // hora del servidor: la de verdad
                CapturedAtUtc = request.CapturedAtUtc, // hora del dispositivo: metadato
                // El rol se congela al enviar: un ascenso posterior no reetiqueta el historial
                SenderRole = Roles.PrimaryRole(user),
                IsSystem = false,
                IdempotencyKey = string.IsNullOrEmpty(request.IdempotencyKey) ? null : request.IdempotencyKey,
            };
            try
            {
                await orders.AddMessageAsync(message);
            }
            catch (DbUpdateException) when (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                // Carrera entre dos reintentos simultáneos: el índice único la corta
                var existing = await orders.FindMessageByIdempotencyKeyAsync(request.IdempotencyKey);
                if (existing is not null) return Results.Ok(existing.ToDto());
                throw;
            }

            // Push a las partes de la orden (best-effort; nunca rompe el envío)
            var senderName = message.User?.DisplayName ?? message.User?.Email ?? "";
            await notify.NotifyOrderMessageAsync(id, message.UserId, senderName, message.Content);

            return Results.Created($"{ApiRoutes.Orders}/{id}/messages", message.ToDto());
        });

        return app;
    }

}
