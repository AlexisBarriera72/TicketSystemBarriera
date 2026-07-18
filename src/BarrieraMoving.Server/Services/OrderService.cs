using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Enums;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

// Using Primary Constructor de C# 14
public class OrderService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
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

    // Actualiza estado y/o conductor asignado, registrando el evento en el chat de la orden
    public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? newDriverId = null, string? performerId = null)
    {
        using var context = dbFactory.CreateDbContext();

        var order = await context.Orders
            .Include(o => o.AssignedDriver)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null) return;

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
                CreatedAt = DateTime.UtcNow
            };

            context.Messages.Add(systemMessage);
            order.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
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

    // --- REPORTES Y ESTADÍSTICAS (dashboard del jefe) ---
    public async Task<Dictionary<string, int>> GetOrderStatsAsync()
    {
        using var context = dbFactory.CreateDbContext();
        return new Dictionary<string, int>
        {
            ["Total"] = await context.Orders.CountAsync(),
            ["Active"] = await context.Orders.CountAsync(o =>
                o.Status != OrderStatus.Completed && o.Status != OrderStatus.PendingSignature),
            ["PendingSignature"] = await context.Orders.CountAsync(o => o.Status == OrderStatus.PendingSignature),
            ["Completed"] = await context.Orders.CountAsync(o => o.Status == OrderStatus.Completed),
            ["Urgent"] = await context.Orders.CountAsync(o => o.Priority == OrderPriority.Urgent)
        };
    }

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
    }

    public async Task<List<Message>> GetMessagesAsync(int orderId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.Messages
            .Include(m => m.User)
            .Where(m => m.OrderId == orderId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    // Reporte Excel con el resumen de órdenes, usando ClosedXML
    public async Task<byte[]> GenerateExcelReportAsync()
    {
        using var context = dbFactory.CreateDbContext();
        var orders = await context.Orders
            .Include(o => o.Author)
            .Include(o => o.AssignedDriver)
            .Include(o => o.Category)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Resumen de Órdenes");

        var headers = new[] { "ID", "Título", "Tipo", "Prioridad", "Estado", "Creado", "Completado",
            "Tiempo (Días)", "Cliente", "Conductor" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var o in orders)
        {
            worksheet.Cell(row, 1).Value = o.Id;
            worksheet.Cell(row, 2).Value = o.Title;
            worksheet.Cell(row, 3).Value = o.Category?.Name ?? "N/A";
            worksheet.Cell(row, 4).Value = o.Priority.ToString();
            worksheet.Cell(row, 5).Value = o.Status.ToString();
            worksheet.Cell(row, 6).Value = o.CreatedAt;
            if (o.Status == OrderStatus.Completed && o.UpdatedAt.HasValue)
            {
                worksheet.Cell(row, 7).Value = o.UpdatedAt.Value;
                var duration = o.UpdatedAt.Value - o.CreatedAt;
                worksheet.Cell(row, 8).Value = Math.Round(duration.TotalDays, 2);
            }
            worksheet.Cell(row, 9).Value = o.Author?.Email;
            worksheet.Cell(row, 10).Value = o.AssignedDriver?.Email ?? "No asignado";
            row++;
        }
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
