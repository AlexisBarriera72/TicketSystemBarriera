using System.Net.Http.Json;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Mobile.Services;

// Login / logout y estado de sesión. El refresh automático vive en AuthMessageHandler.
public class AuthService(TokenStore tokenStore, IHttpClientFactory httpClientFactory)
{
    public async Task<bool> IsLoggedInAsync() =>
        await tokenStore.GetRefreshTokenAsync() is not null;

    public Task<UserSummaryDto?> GetCurrentUserAsync() => tokenStore.GetUserAsync();

    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        try
        {
            var client = CreatePlainClient();
            var response = await client.PostAsJsonAsync(
                $"{ApiRoutes.Auth}/login", new LoginRequest(email, password), ApiJson.Options);

            if (!response.IsSuccessStatusCode)
            {
                return (false, "Email o contraseña incorrectos.");
            }

            var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(ApiJson.Options);
            if (tokens is null)
            {
                return (false, "Respuesta inválida del servidor.");
            }

            await tokenStore.SaveAsync(tokens);
            return (true, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            return (false, $"No se pudo conectar al servidor ({ApiOptions.BaseUrl}). {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await tokenStore.GetRefreshTokenAsync();
        if (refreshToken is not null)
        {
            try
            {
                var client = CreatePlainClient();
                await client.PostAsJsonAsync(
                    $"{ApiRoutes.Auth}/logout", new RefreshRequest(refreshToken), ApiJson.Options);
            }
            catch
            {
                // Sin conexión igual cerramos la sesión local; el token expira solo en el servidor
            }
        }
        await tokenStore.ClearAsync();
    }

    private HttpClient CreatePlainClient()
    {
        var client = httpClientFactory.CreateClient("plain");
        client.BaseAddress = new Uri(ApiOptions.BaseUrl);
        return client;
    }
}
