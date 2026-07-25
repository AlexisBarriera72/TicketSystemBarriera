using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

// Fichajes (clock-in / clock-out). Reglas del negocio SIEMPRE en el servidor:
// - la hora la pone el servidor (DateTime.UtcNow), nunca el teléfono
// - una sola jornada abierta por empleado (además garantizada por índice único filtrado)
// - jornada olvidada: al siguiente clock-in se cierra al tope máximo y se marca
//   AutoClosed para que la oficina revise las horas reales
public class TimeService(IDbContextFactory<ApplicationDbContext> dbFactory, IConfiguration config) : ITimeService
{
    private readonly double _maxShiftHours =
        double.TryParse(config["Time:MaxShiftHours"], out var h) && h > 0 ? h : 12;

    public async Task<TimeEntry?> GetOpenEntryAsync(string userId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.TimeEntries
            .FirstOrDefaultAsync(t => t.UserId == userId && t.ClockOutUtc == null);
    }

    public async Task<(TimeEntry? Entry, string? Error)> ClockInAsync(string userId, double? latitude, double? longitude,
        DateTime? capturedAtUtc = null, string? idempotencyKey = null)
    {
        using var context = dbFactory.CreateDbContext();

        // Reintento de la cola offline que ya llegó: devolver el fichaje original
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var replay = await context.TimeEntries
                .FirstOrDefaultAsync(t => t.ClockInIdempotencyKey == idempotencyKey);
            if (replay is not null) return (replay, null);
        }

        var now = DateTime.UtcNow;
        var maxShift = TimeSpan.FromHours(_maxShiftHours);

        var open = await context.TimeEntries
            .FirstOrDefaultAsync(t => t.UserId == userId && t.ClockOutUtc == null);

        if (open is not null)
        {
            if (now - open.ClockInUtc < maxShift)
            {
                return (null, "Ya tienes una jornada abierta.");
            }
            // Olvidó fichar la salida: cerrar la jornada vieja al tope máximo y marcarla.
            // No inventamos la hora real — la oficina la verifica (AutoClosed visible).
            open.ClockOutUtc = open.ClockInUtc + maxShift;
            open.AutoClosed = true;
        }

        var entry = new TimeEntry
        {
            UserId = userId,
            ClockInUtc = now, // hora del SERVIDOR: la de nómina, siempre
            ClockInCapturedAtUtc = capturedAtUtc, // hora del dispositivo: metadato visible
            ClockInLatitude = latitude,
            ClockInLongitude = longitude,
            ClockInIdempotencyKey = string.IsNullOrEmpty(idempotencyKey) ? null : idempotencyKey,
        };
        context.TimeEntries.Add(entry);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Carrera entre dos clock-in simultáneos (o dos reintentos): los índices únicos la cortan
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var replay = await context.TimeEntries
                    .FirstOrDefaultAsync(t => t.ClockInIdempotencyKey == idempotencyKey);
                if (replay is not null) return (replay, null);
            }
            return (null, "Ya tienes una jornada abierta.");
        }
        return (entry, null);
    }

    public async Task<(TimeEntry? Entry, string? Error)> ClockOutAsync(string userId, double? latitude, double? longitude,
        DateTime? capturedAtUtc = null, string? idempotencyKey = null)
    {
        using var context = dbFactory.CreateDbContext();

        // Reintento que ya llegó: devolver el fichaje ya cerrado
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var replay = await context.TimeEntries
                .FirstOrDefaultAsync(t => t.ClockOutIdempotencyKey == idempotencyKey);
            if (replay is not null) return (replay, null);
        }

        var open = await context.TimeEntries
            .FirstOrDefaultAsync(t => t.UserId == userId && t.ClockOutUtc == null);

        if (open is null)
        {
            // Caso honesto de la cola offline: si la jornada ya se auto-cerró (>12 h),
            // el clock-out en cola falla VISIBLEMENTE y la oficina lo resuelve.
            return (null, "No tienes ninguna jornada abierta.");
        }

        open.ClockOutUtc = DateTime.UtcNow;
        open.ClockOutCapturedAtUtc = capturedAtUtc;
        open.ClockOutLatitude = latitude;
        open.ClockOutLongitude = longitude;
        open.ClockOutIdempotencyKey = string.IsNullOrEmpty(idempotencyKey) ? null : idempotencyKey;
        await context.SaveChangesAsync();
        return (open, null);
    }

    // Quién está trabajando ahora mismo (dashboard del jefe)
    public async Task<List<TimeEntry>> GetActiveEntriesAsync()
    {
        using var context = dbFactory.CreateDbContext();
        return await context.TimeEntries
            .Include(t => t.User)
            .Where(t => t.ClockOutUtc == null)
            .OrderBy(t => t.ClockInUtc)
            .ToListAsync();
    }

    public async Task<List<TimeEntry>> GetRecentEntriesAsync(int days, string? userId = null)
    {
        using var context = dbFactory.CreateDbContext();
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await context.TimeEntries
            .Include(t => t.User)
            .Where(t => t.ClockInUtc >= cutoff && (userId == null || t.UserId == userId))
            .OrderByDescending(t => t.ClockInUtc)
            .ToListAsync();
    }
}
