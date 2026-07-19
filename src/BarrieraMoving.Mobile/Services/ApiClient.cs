using System.Net;
using System.Net.Http.Json;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Mobile.Services;

// Cliente tipado de /api/v1. Solo DTOs compartidos — nada de EF ni SQL (regla dura).
public class ApiClient(HttpClient http)
{
    // El servidor ya filtra por rol (Admin/Oficina todo, conductor lo suyo, cliente lo que creó)
    public async Task<List<OrderDto>> GetOrdersAsync() =>
        await http.GetFromJsonAsync<List<OrderDto>>(ApiRoutes.Orders, ApiJson.Options) ?? [];

    public async Task<OrderDto?> GetOrderAsync(int id)
    {
        var response = await http.GetAsync($"{ApiRoutes.Orders}/{id}");
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(ApiJson.Options);
    }

    // --- FICHAJE (la hora la pone el servidor; aquí solo van coordenadas opcionales) ---

    public async Task<TimeEntryDto?> GetCurrentTimeEntryAsync()
    {
        var response = await http.GetAsync($"{ApiRoutes.Time}/current");
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TimeEntryDto>(ApiJson.Options);
    }

    public Task<(TimeEntryDto? Entry, string? Error)> ClockInAsync(double? latitude, double? longitude) =>
        ClockAsync("clock-in", latitude, longitude);

    public Task<(TimeEntryDto? Entry, string? Error)> ClockOutAsync(double? latitude, double? longitude) =>
        ClockAsync("clock-out", latitude, longitude);

    private async Task<(TimeEntryDto?, string?)> ClockAsync(string action, double? latitude, double? longitude)
    {
        var response = await http.PostAsJsonAsync(
            $"{ApiRoutes.Time}/{action}", new ClockRequest(latitude, longitude), ApiJson.Options);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var message = await response.Content.ReadFromJsonAsync<string>(ApiJson.Options);
            return (null, message ?? "Conflicto de fichaje.");
        }
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeEntryDto>(ApiJson.Options), null);
    }

    public async Task<List<TimeEntryDto>> GetTimeHistoryAsync(int days = 14) =>
        await http.GetFromJsonAsync<List<TimeEntryDto>>($"{ApiRoutes.Time}/history?days={days}", ApiJson.Options) ?? [];
}
