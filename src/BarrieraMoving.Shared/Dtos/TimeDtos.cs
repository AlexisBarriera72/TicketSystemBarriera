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
    bool AutoClosed);

// Coordenadas opcionales: la ubicación NUNCA bloquea el fichaje.
// La hora la pone SIEMPRE el servidor — el cliente no envía timestamps.
public record ClockRequest(double? Latitude, double? Longitude);
