using SQLite;

namespace BarrieraMoving.Mobile.Services;

public enum OutboxKind { Message = 0, Photo = 1, ClockIn = 2, ClockOut = 3 }

// Pending → Sending → (Sent = fila borrada) | Failed (visible y reintentables — jamás se descarta solo)
public enum OutboxStatus { Pending = 0, Sending = 1, Failed = 2 }

// Un envío pendiente. El Id (GUID) viaja al servidor como clave de idempotencia:
// un reintento de algo que en realidad sí llegó devuelve el original, no duplica.
public class OutboxItem
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public OutboxKind Kind { get; set; }
    public int OrderId { get; set; }          // 0 en fichajes
    public string? Text { get; set; }         // contenido del mensaje
    public string? FilePath { get; set; }     // JPEG comprimido en disco (fotos)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Hora del DISPOSITIVO al pulsar (metadato no fiable; el servidor sella la suya)
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;

    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime NextAttemptUtc { get; set; } = DateTime.UtcNow;
}
