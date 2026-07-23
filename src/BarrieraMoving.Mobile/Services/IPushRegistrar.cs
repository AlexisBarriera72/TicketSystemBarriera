namespace BarrieraMoving.Mobile.Services;

// Registro del token push del dispositivo con el backend. Implementación por
// plataforma (Android = FCM hoy; iOS = APNs en el futuro). Todo best-effort:
// jamás lanza — si falla, el login/logout siguen su curso.
public interface IPushRegistrar
{
    Task RegisterAsync();
    Task UnregisterAsync();
}

// Fallback para plataformas sin push (o builds donde no se compila FCM).
public sealed class NoOpPushRegistrar : IPushRegistrar
{
    public Task RegisterAsync() => Task.CompletedTask;
    public Task UnregisterAsync() => Task.CompletedTask;
}
