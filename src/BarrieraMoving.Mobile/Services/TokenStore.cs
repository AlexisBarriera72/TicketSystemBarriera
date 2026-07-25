using System.Text.Json;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Mobile.Services;

// Tokens SIEMPRE en SecureStorage (Android Keystore) — nunca Preferences ni archivos.
// Cachea en memoria para no tocar SecureStorage en cada petición HTTP.
public class TokenStore
{
    private const string AccessKey = "auth_access_token";
    private const string RefreshKey = "auth_refresh_token";
    private const string UserKey = "auth_user_json";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _accessToken;
    private string? _refreshToken;
    private UserSummaryDto? _user;
    private bool _loaded;

    public async Task<string?> GetAccessTokenAsync()
    {
        await EnsureLoadedAsync();
        return _accessToken;
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        await EnsureLoadedAsync();
        return _refreshToken;
    }

    public async Task<UserSummaryDto?> GetUserAsync()
    {
        await EnsureLoadedAsync();
        return _user;
    }

    public async Task SaveAsync(TokenResponse tokens)
    {
        await _gate.WaitAsync();
        try
        {
            _accessToken = tokens.AccessToken;
            _refreshToken = tokens.RefreshToken;
            _user = tokens.User;
            _loaded = true;
            await SecureStorage.Default.SetAsync(AccessKey, tokens.AccessToken);
            await SecureStorage.Default.SetAsync(RefreshKey, tokens.RefreshToken);
            await SecureStorage.Default.SetAsync(UserKey, JsonSerializer.Serialize(tokens.User, ApiJson.Options));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _accessToken = null;
            _refreshToken = null;
            _user = null;
            _loaded = true;
            SecureStorage.Default.Remove(AccessKey);
            SecureStorage.Default.Remove(RefreshKey);
            SecureStorage.Default.Remove(UserKey);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await _gate.WaitAsync();
        try
        {
            if (_loaded) return;
            _accessToken = await SecureStorage.Default.GetAsync(AccessKey);
            _refreshToken = await SecureStorage.Default.GetAsync(RefreshKey);
            var userJson = await SecureStorage.Default.GetAsync(UserKey);
            _user = userJson is null ? null : JsonSerializer.Deserialize<UserSummaryDto>(userJson, ApiJson.Options);
            _loaded = true;
        }
        finally
        {
            _gate.Release();
        }
    }
}
