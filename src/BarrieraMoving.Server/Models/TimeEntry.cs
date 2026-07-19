namespace BarrieraMoving.Server.Models;

using BarrieraMoving.Server.Data;

// Registro de clock-in / clock-out de un empleado (Fase 3 lo usa; la tabla nace aquí
// para no necesitar otra migración)
public class TimeEntry
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public DateTime ClockInUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClockOutUtc { get; set; }
    public double? ClockInLatitude { get; set; }
    public double? ClockInLongitude { get; set; }
    public double? ClockOutLatitude { get; set; }
    public double? ClockOutLongitude { get; set; }

    // true = el servidor cerró la jornada al tope máximo porque el empleado
    // olvidó fichar la salida; la oficina debe verificar las horas reales
    public bool AutoClosed { get; set; }
}
