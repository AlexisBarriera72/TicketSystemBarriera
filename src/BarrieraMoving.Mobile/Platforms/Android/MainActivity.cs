using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using BarrieraMoving.Mobile.Services;

namespace BarrieraMoving.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    // App cerrada: la notificación la abre y los extras llegan en el intent inicial
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandlePushIntent(Intent);
    }

    // App ya abierta en segundo plano: el intent llega por aquí
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandlePushIntent(intent);
    }

    // Traduce los extras "push_*" que puso BarrieraFirebaseMessagingService en una
    // ruta pendiente que consumirá la capa Blazor cuando pueda navegar.
    private static void HandlePushIntent(Intent? intent)
    {
        var extras = intent?.Extras;
        if (extras is null) return;

        var data = new Dictionary<string, string?>();
        foreach (var key in extras.KeySet() ?? [])
        {
            if (key.StartsWith("push_", StringComparison.Ordinal))
            {
                data[key["push_".Length..]] = extras.GetString(key);
            }
        }
        if (data.Count == 0) return;

        var deepLink = IPlatformApplication.Current?.Services.GetService<DeepLinkState>();
        deepLink?.SetFromPush(data);
    }
}
