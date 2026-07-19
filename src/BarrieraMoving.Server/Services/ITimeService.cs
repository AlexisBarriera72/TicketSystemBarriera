using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

public interface ITimeService
{
    Task<TimeEntry?> GetOpenEntryAsync(string userId);
    Task<(TimeEntry? Entry, string? Error)> ClockInAsync(string userId, double? latitude, double? longitude);
    Task<(TimeEntry? Entry, string? Error)> ClockOutAsync(string userId, double? latitude, double? longitude);
    Task<List<TimeEntry>> GetActiveEntriesAsync();
    Task<List<TimeEntry>> GetRecentEntriesAsync(int days, string? userId = null);
}
