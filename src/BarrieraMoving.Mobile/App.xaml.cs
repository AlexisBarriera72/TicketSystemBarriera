using BarrieraMoving.Mobile.Services;

namespace BarrieraMoving.Mobile;

public partial class App : Application
{
	private readonly OutboxService _outbox;

	public App(OutboxService outbox)
	{
		InitializeComponent();
		_outbox = outbox;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage()) { Title = "Barriera Moving" };
		// Al volver a primer plano, intentar vaciar la cola offline
		window.Resumed += (_, _) => _ = _outbox.FlushAsync();
		return window;
	}
}
