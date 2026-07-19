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

    public async Task<(TimeEntry? Entry, string? Error)> ClockInAsync(string userId, double? latitude, double? longitude)
    {
        using var context = dbFactory.CreateDbContext();
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
            ClockInUtc = now,
            ClockInLatitude = latitude,
            ClockInLongitude = longitude,
        };
        context.TimeEntries.Add(entry);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Carrera entre dos clock-in simultáneos: el índice único la corta
            return (null, "Ya tienes una jornada abierta.");
        }
        return (entry, null);
    }

    public async Task<(TimeEntry? Entry, string? Error)> ClockOutAsync(string userId, double? latitude, double? longitude)
    {
        using var context = dbFactory.CreateDbContext();

        var open = await context.TimeEntries
            .FirstOrDefaultAsync(t => t.UserId == userId && t.ClockOutUtc == null);

        if (open is null)
        {
            return (null, "No tienes ninguna jornada abierta.");
        }

        open.ClockOutUtc = DateTime.UtcNow;
        open.ClockOutLatitude = latitude;
        open.ClockOutLongitude = longitude;
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
