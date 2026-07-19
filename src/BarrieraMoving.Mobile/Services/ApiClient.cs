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
}
