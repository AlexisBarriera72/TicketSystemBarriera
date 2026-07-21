using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

// Using Primary Constructor de C# 14
public class OrderService(IDbContextFactory<ApplicationDbContext> dbFactory) : IOrderService
{
    // Transiciones válidas del flujo de una mudanza. Admin/Oficina pueden saltárselas
    // (bypassValidation); el conductor no. En la Fase 6, PendingSignature → Completed
    // exigirá además documento firmado + aprobación de oficina.
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Requested] = [OrderStatus.Assigned],
        [OrderStatus.Assigned] = [OrderStatus.EnRoute],
        [OrderStatus.EnRoute] = [OrderStatus.InProgress],
        [OrderStatus.InProgress] = [OrderStatus.PendingSignature],
        [OrderStatus.PendingSignature] = [OrderStatus.Completed],
        [OrderStatus.Completed] = [],
    };

    public static bool IsValidTransition(OrderStatus from, OrderStatus to) =>
        AllowedTransitions.TryGetValue(from, out var next) && next.Contains(to);

    // Órdenes donde el usuario es el autor (cliente) o el conductor asignado
    public async Task<List<Order>> GetMyWorkAndRequestsAsync(string userId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.Orders
            .Include(o => o.Category)
            .Include(o => o.Author)
            .Include(o => o.AssignedDriver)
            .Where(o => o.AuthorId == userId || o.AssignedDriverId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        using var context = dbFactory.CreateDbContext();
        return await context.Categories.ToListAsync();
    }

    public async Task CreateOrderAsync(Order order)
    {
        using var context = dbFactory.CreateDbContext();
        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }

    // Lista de órdenes según el rol: Admin/Oficina ven todo, el conductor ve lo suyo,
    // el cliente ve solo lo que creó
    public async Task<List<Order>> GetOrdersForUserAsync(ClaimsPrincipal user)
    {
        using var context = dbFactory.CreateDbContext();

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var query = context.Orders
            .Include(o => o.Category)
            .Include(o => o.Author)
            .Include(o => o.AssignedDriver)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Office))
        {
            return await query.ToListAsync();
        }
        if (user.IsInRole(Roles.Driver))
        {
            return await query.Where(o => o.AssignedDriverId == userId).ToListAsync();
        }
        return await query.Where(o => o.AuthorId == userId).ToListAsync();
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        using var context = dbFactory.CreateDbContext();
        return await context.Orders
            .Include(o => o.Category)
            .Include(o => o.Author)
            .Include(o => o.AssignedDriver)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    // Actualiza estado y/o conductor asignado, registrando el evento en el chat de la orden.
    // Devuelve (false, motivo) si la orden no existe, la transición no está permitida
    // o falta el documento firmado+aprobado para completar.
    public async Task<(bool Ok, string? Error)> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? newDriverId = null,
        string? performerId = null, bool bypassValidation = false)
    {
        using var context = dbFactory.CreateDbContext();

        var order = await context.Orders
            .Include(o => o.AssignedDriver)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null) return (false, "La orden no existe.");

        string systemLog = "";
        var oldStatus = order.Status;
        var oldDriverId = order.AssignedDriverId;

        // ESCENARIO 1: asignación de conductor (Admin/Oficina)
        if (!string.IsNullOrEmpty(newDriverId) && newDriverId != oldDriverId)
        {
            order.AssignedDriverId = newDriverId;
            if (order.Status == OrderStatus.Requested && newStatus == oldStatus)
            {
                // La asignación avanza la orden automáticamente a "Assigned"
                newStatus = OrderStatus.Assigned;
            }
            systemLog = "[SISTEMA] Se ha asignado un conductor a esta orden.";
        }

        // ESCENARIO 2: cambio de estado
        if (newStatus != oldStatus)
        {
            if (!bypassValidation && !IsValidTransition(oldStatus, newStatus))
            {
                return (false, $"Transición no permitida: {oldStatus} → {newStatus}.");
            }

            // GATE LEGAL (fase 6): Completed exige un documento firmado por el
            // cliente Y aprobado por la oficina. Este check NO se salta ni con
            // bypassValidation — es la protección del cliente ante reclamaciones,
            // no una simple validación de flujo.
            if (newStatus == OrderStatus.Completed)
            {
                var hasApprovedDoc = await context.SignatureDocuments
                    .AnyAsync(d => d.OrderId == orderId && d.Status == SignatureDocStatus.Approved);
                if (!hasApprovedDoc)
                {
                    return (false, "No se puede completar la orden: falta un documento firmado por el cliente y aprobado por la oficina.");
                }
            }

            order.Status = newStatus;
            systemLog = string.IsNullOrEmpty(systemLog)
                ? $"[EVENTO] El estado cambió de {oldStatus} a {newStatus}."
                : systemLog + $" El estado actual es {newStatus}.";
        }

        if (!string.IsNullOrEmpty(systemLog))
        {
            var systemMessage = new Message
            {
                OrderId = orderId,
                UserId = performerId ?? (newDriverId ?? order.AuthorId),
                Content = systemLog,
                CreatedAt = DateTime.UtcNow,
                IsSystem = true
            };

            context.Messages.Add(systemMessage);
            order.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
        return (true, null);
    }

    public async Task<List<ApplicationUser>> GetUsersByRoleAsync(string roleName)
    {
        using var context = dbFactory.CreateDbContext();

        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null) return [];

        var userIds = await context.UserRoles
            .Where(ur => ur.RoleId == role.Id)
            .Select(ur => ur.UserId)
            .ToListAsync();

        return await context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();
    }

    // Obtener una orden específica con todos sus detalles y relaciones
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.Orders
            .Include(o => o.Category)
            .Include(o => o.Author)
            .Include(o => o.AssignedDriver)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    // Listar los conductores disponibles para asignar
    public Task<List<ApplicationUser>> GetDriversAsync() => GetUsersByRoleAsync(Roles.Driver);

    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        using var context = dbFactory.CreateDbContext();
        return await context.Users.OrderBy(u => u.Email).ToListAsync();
    }

    // --- CHAT DE LA ORDEN ---
    public async Task AddMessageAsync(Message message)
    {
        using var context = dbFactory.CreateDbContext();
        context.Messages.Add(message);
        await context.SaveChangesAsync();
        // Cargar el remitente para que la respuesta de la API traiga el nombre
        await context.Entry(message).Reference(m => m.User).LoadAsync();
    }

    // Para servir la foto adjunta: el mensaje con su orden (el ACL se evalúa sobre la orden)
    public async Task<Message?> GetMessageWithOrderAsync(int messageId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.Messages
            .Include(m => m.Order)
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }

    public async Task<Message?> FindMessageByIdempotencyKeyAsync(string idempotencyKey)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.Messages
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey);
    }

    // Paginado: por defecto los últimos `take`; beforeId = página anterior
    // ("cargar mensajes antiguos"); afterId = solo los nuevos (polling delta).
    public async Task<List<Message>> GetMessagesAsync(int orderId, int take = 50, int? beforeId = null, int? afterId = null)
    {
        using var context = dbFactory.CreateDbContext();
        take = Math.Clamp(take, 1, 200);

        var query = context.Messages
            .Include(m => m.User)
            .Where(m => m.OrderId == orderId);

        if (afterId is not null)
        {
            return await query
                .Where(m => m.Id > afterId)
                .OrderBy(m => m.Id)
                .Take(take)
                .ToListAsync();
        }

        if (beforeId is not null)
        {
            query = query.Where(m => m.Id < beforeId);
        }

        var page = await query
            .OrderByDescending(m => m.Id)
            .Take(take)
            .ToListAsync();
        page.Reverse(); // devolver siempre en orden cronológico
        return page;
    }
}
