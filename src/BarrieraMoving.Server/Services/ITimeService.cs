using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

public interface ITimeService
{
    Task<TimeEntry?> GetOpenEntryAsync(string userId);
    Task<(TimeEntry? Entry, string? Error)> ClockInAsync(string userId, double? latitude, double? longitude,
        DateTime? capturedAtUtc = null, string? idempotencyKey = null);
    Task<(TimeEntry? Entry, string? Error)> ClockOutAsync(string userId, double? latitude, double? longitude,
        DateTime? capturedAtUtc = null, string? idempotencyKey = null);
    Task<List<TimeEntry>> GetActiveEntriesAsync();
    Task<List<TimeEntry>> GetRecentEntriesAsync(int days, string? userId = null);
}
