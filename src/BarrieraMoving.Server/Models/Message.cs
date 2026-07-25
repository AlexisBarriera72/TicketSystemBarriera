namespace BarrieraMoving.Server.Models;

using BarrieraMoving.Server.Data;
using BarrieraMoving.Shared.Enums;

// Mensaje del chat de una orden (antes "TicketComment")
public class Message
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Rol del remitente EN EL MOMENTO de enviar. Los roles cambian (conductor
    // ascendido a oficina); el historial del chat no debe reetiquetarse.
    public string? SenderRole { get; set; }

    // true = mensaje de sistema/auditoría (cambios de estado, asignaciones)
    public bool IsSystem { get; set; }

    // Foto adjunta: claves relativas dentro de IPhotoStorage (nunca rutas absolutas).
    // La imagen se sirve SOLO por el endpoint con el mismo ACL de la orden.
    public string? AttachmentPath { get; set; }
    public string? AttachmentThumbPath { get; set; }

    // Etapa de la foto (Recogida / Entrega). Null en mensajes sin foto.
    public PhotoStage? Stage { get; set; }

    // GPS capturado DELIBERADAMENTE al enviar (el EXIF original se elimina siempre)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Hora del dispositivo al capturar/escribir (metadato NO fiable; CreatedAt
    // sigue siendo la hora del servidor al recibir)
    public DateTime? CapturedAtUtc { get; set; }

    // Clave de la cola offline: un reintento del mismo envío no duplica el mensaje
    public string? IdempotencyKey { get; set; }
}
