using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Mobile.Services;

// Adjunta el Bearer token a cada petición y, ante un 401, refresca el token UNA
// vez y reintenta. El refresh es "single-flight": los refresh tokens del servidor
// rotan y son de un solo uso, así que dos peticiones paralelas con 401 no pueden
// llamar a /auth/refresh a la vez — la segunda espera y reutiliza el token nuevo.
public class AuthMessageHandler(TokenStore tokenStore) : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshGate = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await tokenStore.GetAccessTokenAsync();
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        // Bufferizar el body para poder reintentar la misma petición tras el refresh
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync();
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        var newAccessToken = await RefreshAsync(accessToken, cancellationToken);
        if (newAccessToken is null)
        {
            return response; // sesión expirada: TokenStore ya quedó limpio, la UI vuelve al login
        }

        response.Dispose();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> RefreshAsync(string? failedAccessToken, CancellationToken cancellationToken)
    {
        await RefreshGate.WaitAsync(cancellationToken);
        try
        {
            // Otro hilo pudo refrescar mientras esperábamos el semáforo
            var current = await tokenStore.GetAccessTokenAsync();
            if (current is not null && current != failedAccessToken)
            {
                return current;
            }

            var refreshToken = await tokenStore.GetRefreshTokenAsync();
            if (refreshToken is null)
            {
                return null;
            }

            // Cliente "pelado" a propósito: el refresh no debe pasar por este handler
            using var client = new HttpClient { BaseAddress = new Uri(ApiOptions.BaseUrl) };
            using var response = await client.PostAsJsonAsync(
                $"{ApiRoutes.Auth}/refresh", new RefreshRequest(refreshToken), ApiJson.Options, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await tokenStore.ClearAsync();
                return null;
            }

            var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(ApiJson.Options, cancellationToken);
            if (tokens is null)
            {
                await tokenStore.ClearAsync();
                return null;
            }

            await tokenStore.SaveAsync(tokens);
            return tokens.AccessToken;
        }
        catch (HttpRequestException)
        {
            return null; // sin red: dejamos la sesión intacta y devolvemos el 401 original
        }
        finally
        {
            RefreshGate.Release();
        }
    }
}
