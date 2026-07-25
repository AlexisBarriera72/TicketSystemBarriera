using Microsoft.Extensions.Logging;
using BarrieraMoving.Mobile.Services;

namespace BarrieraMoving.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		// Auth + API: el teléfono SOLO habla con /api/v1 por HTTP(S), nunca con SQL
		builder.Services.AddSingleton<TokenStore>();
		builder.Services.AddSingleton<ClockState>();
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<OutboxService>(); // cola offline compartida (mensajes/fotos/fichajes)
		builder.Services.AddSingleton<NotificationState>(); // feed de notificaciones (contador no leído)
		builder.Services.AddSingleton<DeepLinkState>();   // ruta pendiente al pulsar una push
		builder.Services.AddTransient<AuthMessageHandler>();

		// Registro de push por plataforma (Android = FCM; iOS = APNs en el futuro)
#if ANDROID
		builder.Services.AddSingleton<IPushRegistrar, AndroidPushRegistrar>();
#else
		builder.Services.AddSingleton<IPushRegistrar, NoOpPushRegistrar>();
#endif

		// Cliente sin auth para login/refresh/logout (BaseAddress se fija al usarlo,
		// así el cambio de "Servidor" en el login aplica al instante)
		builder.Services.AddHttpClient("plain");

		// Cliente tipado con Bearer + auto-refresh; BaseAddress se evalúa en cada creación
		builder.Services.AddHttpClient<ApiClient>(client =>
				client.BaseAddress = new Uri(ApiOptions.BaseUrl))
			.AddHttpMessageHandler<AuthMessageHandler>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
