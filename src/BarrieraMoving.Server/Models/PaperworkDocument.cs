namespace BarrieraMoving.Server.Models;

using BarrieraMoving.Server.Data;
using BarrieraMoving.Shared.Enums;

// Papeleo obligatorio de la orden (inventario, estado del mobiliario, etc.).
// Forma parte del MISMO paquete de documentos que la firma (decisión Opción A):
// una sola cola de revisión de oficina, un solo gate de cierre, y todo acaba
// ensamblado en UN único PDF que es lo que el cliente firma.
// Registro legal: sin endpoints de borrado; reemplazar = documento nuevo,
// el anterior queda como Replaced.
public class PaperworkDocument
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    // Clave del slot configurado (Paperwork:Slots en appsettings) — las etiquetas
    // y el número de slots son configuración, no código
    public required string SlotKey { get; set; }

    public required string UploadedByUserId { get; set; }
    public ApplicationUser? UploadedBy { get; set; }

    // Clave relativa en IPhotoStorage (JPEG re-codificado sin EXIF, o PDF tal cual)
    public required string FilePath { get; set; }
    public string? ThumbPath { get; set; } // solo imágenes
    public bool IsPdf { get; set; }

    // SHA-256 del archivo almacenado: se imprime en el manifiesto del paquete
    // firmado, atando el papeleo a la firma del cliente
    public required string ContentHash { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow; // hora del servidor
    public DateTime? CapturedAtUtc { get; set; }                  // hora del dispositivo (metadato)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public PaperworkStatus Status { get; set; } = PaperworkStatus.Attached;
    public string? RejectReason { get; set; } // accionable: qué rehacer y por qué
    public string? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    public string? IdempotencyKey { get; set; } // cola offline: sin duplicados
}
