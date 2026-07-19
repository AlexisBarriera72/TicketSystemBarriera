using Android.App;
using Android.Runtime;

namespace BarrieraMoving.Mobile;

// Solo en DEBUG se permite HTTP sin TLS: Android no confía en el certificado de
// desarrollo de ASP.NET, así que en desarrollo hablamos http con 10.0.2.2 / la LAN.
// En Release el tráfico en claro queda prohibido (la API de producción irá por HTTPS).
#if DEBUG
[Application(UsesCleartextTraffic = true)]
#else
[Application]
#endif
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
