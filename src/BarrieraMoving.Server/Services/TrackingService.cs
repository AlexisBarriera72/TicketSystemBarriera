using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

public sealed class TrackingService(IDbContextFactory<ApplicationDbContext> dbFactory)
    : ITrackingService
{
    // Caduca 30 días DESPUÉS de completar la mudanza: el cliente sigue pudiendo
    // consultar un tiempo razonable, pero el enlace no vive para siempre.
    private static readonly TimeSpan KeepAfterCompleted = TimeSpan.FromDays(30);

    public async Task<string?> EnsureTokenAsync(int orderId)
    {
        using var db = dbFactory.CreateDbContext();
        var order = await db.Orders.FindAsync(orderId);
        if (order is null) return null;

        if (string.IsNullOrEmpty(order.TrackingToken))
        {
            // 32 bytes aleatorios en Base64 URL-safe: no adivinable por fuerza bruta
            order.TrackingToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            order.TrackingTokenCreatedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        return order.TrackingToken;
    }

    public async Task RevokeAsync(int orderId)
    {
        using var db = dbFactory.CreateDbContext();
        var order = await db.Orders.FindAsync(orderId);
        if (order is null) return;
        order.TrackingToken = null;
        order.TrackingTokenCreatedUtc = null;
        await db.SaveChangesAsync();
    }

    public async Task<string?> GetTokenAsync(int orderId)
    {
        using var db = dbFactory.CreateDbContext();
        return (await db.Orders.FindAsync(orderId))?.TrackingToken;
    }

    public async Task<TrackingInfo?> ResolveAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 20) return null;

        using var db = dbFactory.CreateDbContext();
        var order = await db.Orders
            .Include(o => o.AssignedDriver)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.TrackingToken == token);

        if (order is null) return null;

        // Caducidad: 30 días desde que se completó
        if (order.Status == OrderStatus.Completed)
        {
            var completedAt = order.UpdatedAt ?? order.CreatedAt;
            if (DateTime.UtcNow - completedAt > KeepAfterCompleted) return null;
        }

        // Solo el NOMBRE de pila del conductor: identifica a quien llega a la
        // puerta sin publicar el nombre completo de un empleado.
        var driver = order.AssignedDriver?.DisplayName ?? order.AssignedDriver?.Email;
        var firstName = string.IsNullOrWhiteSpace(driver)
            ? null
            : driver.Split(' ', '@')[0];

        return new TrackingInfo(order.Id, order.Title, order.Status,
            order.CreatedAt, order.UpdatedAt, firstName);
    }
}
