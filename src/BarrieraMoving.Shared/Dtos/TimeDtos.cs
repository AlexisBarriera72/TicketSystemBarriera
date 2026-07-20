namespace BarrieraMoving.Shared.Dtos;

public record TimeEntryDto(
    int Id,
    string UserId,
    string? UserName,
    DateTime ClockInUtc,
    DateTime? ClockOutUtc,
    double? ClockInLatitude,
    double? ClockInLongitude,
    double? ClockOutLatitude,
    double? ClockOutLongitude,
    bool AutoClosed,
    DateTime? ClockInCapturedAtUtc,
    DateTime? ClockOutCapturedAtUtc);

// Coordenadas opcionales: la ubicación NUNCA bloquea el fichaje.
// La hora de NÓMINA la pone SIEMPRE el servidor; CapturedAtUtc es la hora del
// dispositivo (metadato no fiable) para que la oficina vea la diferencia cuando
// un fichaje en cola llega tarde. IdempotencyKey: reintentos sin duplicados.
public record ClockRequest(
    double? Latitude,
    double? Longitude,
    DateTime? CapturedAtUtc = null,
    string? IdempotencyKey = null);
