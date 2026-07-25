namespace BarrieraMoving.Shared.Dtos;

// El dispositivo registra su token FCM tras iniciar sesión; lo borra al cerrar sesión.
public record RegisterPushTokenRequest(string Token, string Platform);
public record UnregisterPushTokenRequest(string Token);
