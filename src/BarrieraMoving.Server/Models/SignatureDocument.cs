namespace BarrieraMoving.Server.Models;

using BarrieraMoving.Server.Data;
using BarrieraMoving.Shared.Enums;

// Documento de conformidad firmado por el cliente. Es un REGISTRO LEGAL:
// - inmutable una vez firmado (no existe endpoint de borrado, para nadie)
// - el PDF se guarda SIEMPRE en nuestro almacenamiento (espejo), aunque el
//   proveedor externo tenga el suyo — si algún día se deja de pagar al
//   proveedor, los documentos siguen siendo nuestros
// - la orden no puede llegar a Completed sin un documento Approved (gate en
//   OrderService.UpdateOrderStatusAsync)
public class SignatureDocument
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public required string RequestedByUserId { get; set; } // conductor que inició la firma
    public ApplicationUser? RequestedBy { get; set; }

    public SignatureDocStatus Status { get; set; } = SignatureDocStatus.AwaitingSignature;

    // true = ceremonia sin conexión en el dispositivo (canvas + nombre + GPS + hash);
    // evidencia más débil que la del proveedor — la oficina lo ve marcado al revisar
    public bool IsProvisional { get; set; }

    // Ruta online (proveedor externo)
    public string? ProviderEnvelopeId { get; set; }

    // Identidad del firmante
    public string? SignerName { get; set; }
    public string? SignerEmail { get; set; }

    // GPS capturado deliberadamente en la ceremonia offline
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SignedAtUtc { get; set; }         // hora del SERVIDOR al recibir
    public DateTime? SignedCapturedAtUtc { get; set; } // hora del dispositivo (metadato)

    // PDF firmado espejado en nuestro almacenamiento (clave relativa, jamás URL adivinable)
    public string? PdfPath { get; set; }
    public string? ContentHash { get; set; } // SHA-256 del PDF firmado

    // Revisión de oficina (obligatoria antes de Completed)
    public string? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? RejectReason { get; set; } // accionable: el conductor ve qué rehacer

    public EmailDeliveryStatus EmailStatus { get; set; } = EmailDeliveryStatus.NotSent;
    public string? EmailError { get; set; }

    // Clave de la cola offline del móvil (reintentos sin duplicar documentos)
    public string? IdempotencyKey { get; set; }
}
