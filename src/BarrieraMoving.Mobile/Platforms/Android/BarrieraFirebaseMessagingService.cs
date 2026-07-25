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
        var title = notification?.Title ?? "Transporte Caribe";
        var body = notification?.Body ?? "";
        // message.Data lleva type/orderId/conversationId: viaja al intent para
        // que al pulsar se abra la pantalla concreta, no solo la app.
        ShowNotification(title, body, message.Data);
    }

    private void ShowNotification(string title, string body, IDictionary<string, string>? data)
    {
        var manager = (NotificationManager)GetSystemService(NotificationService)!;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "Notificaciones", NotificationImportance.High);
            manager.CreateNotificationChannel(channel);
        }

        // Al pulsar la notificación se abre la app EN LA PANTALLA correspondiente
        var launch = PackageManager!.GetLaunchIntentForPackage(PackageName!);
        launch?.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        if (data is not null)
        {
            foreach (var kv in data) launch?.PutExtra($"push_{kv.Key}", kv.Value);
        }
        // Cada notificación necesita su propio requestCode: con 0 para todas,
        // UpdateCurrent reescribiría los extras de las anteriores.
        var pending = PendingIntent.GetActivity(this, System.Environment.TickCount, launch,
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
