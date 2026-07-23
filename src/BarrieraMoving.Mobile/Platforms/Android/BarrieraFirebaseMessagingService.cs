using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Firebase.Messaging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using BarrieraMoving.Mobile.Services;

namespace BarrieraMoving.Mobile;

// Servicio FCM: recibe el token (OnNewToken) y los mensajes entrantes
// (OnMessageReceived). Android lo instancia por el IntentFilter; no pasa por DI,
// así que resolvemos servicios desde el proveedor de la app.
[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public sealed class BarrieraFirebaseMessagingService : FirebaseMessagingService
{
    private const string ChannelId = "barriera_default";

    // FCM rota el token; lo guardamos y, si hay sesión, lo re-registramos ya
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        Preferences.Set(AndroidPushRegistrar.TokenPref, token);
        try
        {
            if (IPlatformApplication.Current?.Services.GetService(typeof(ApiClient)) is ApiClient api)
                _ = api.RegisterPushTokenAsync(token, "android");
        }
        catch { /* aún no hay sesión: se registrará al iniciarla */ }
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);
        var notification = message.GetNotification();
        var title = notification?.Title ?? "Barriera Moving";
        var body = notification?.Body ?? "";
        ShowNotification(title, body);
    }

    private void ShowNotification(string title, string body)
    {
        var manager = (NotificationManager)GetSystemService(NotificationService)!;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "Notificaciones", NotificationImportance.High);
            manager.CreateNotificationChannel(channel);
        }

        // Al pulsar la notificación se abre la app
        var launch = PackageManager!.GetLaunchIntentForPackage(PackageName!);
        launch?.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        var pending = PendingIntent.GetActivity(this, 0, launch,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetSmallIcon(global::Android.Resource.Drawable.SymDefAppIcon)
            .SetAutoCancel(true)
            .SetContentIntent(pending)
            .SetPriority((int)NotificationPriority.High);

        manager.Notify(System.Environment.TickCount, builder.Build());
    }
}
