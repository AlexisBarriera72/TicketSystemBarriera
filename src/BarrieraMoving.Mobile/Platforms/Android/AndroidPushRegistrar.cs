using Firebase.Messaging;
using Microsoft.Extensions.DependencyInjection;
using BarrieraMoving.Mobile.Services;

namespace BarrieraMoving.Mobile;

// Registro FCM en Android. Resuelve ApiClient POR LLAMADA (cliente tipado transitorio)
// para no capturar un HttpClient viejo en este singleton.
public sealed class AndroidPushRegistrar(IServiceProvider services) : IPushRegistrar
{
    public const string TokenPref = "fcm_token";

    public async Task RegisterAsync()
    {
        try
        {
            // Permiso de notificaciones (Android 13+). Aunque se deniegue seguimos
            // registrando el token: el usuario puede activarlo luego en Ajustes.
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try { await Permissions.RequestAsync<Permissions.PostNotifications>(); }
                catch { /* plataformas < 13 no lo piden */ }
            });

            var tokenObj = await FirebaseMessaging.Instance.GetToken().ToAwaitable();
            var token = tokenObj?.ToString();
            if (string.IsNullOrWhiteSpace(token)) return;

            Preferences.Set(TokenPref, token);
            await services.GetRequiredService<ApiClient>().RegisterPushTokenAsync(token, "android");
        }
        catch { /* best-effort: se reintenta en el próximo login/arranque */ }
    }

    public async Task UnregisterAsync()
    {
        try
        {
            var token = Preferences.Get(TokenPref, null);
            if (string.IsNullOrWhiteSpace(token)) return;

            await services.GetRequiredService<ApiClient>().UnregisterPushTokenAsync(token);
            Preferences.Remove(TokenPref);
        }
        catch { /* al desinstalar, FCM invalida el token igualmente */ }
    }
}
