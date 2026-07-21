namespace BarrieraMoving.Server.Models;

using BarrieraMoving.Server.Data;

// Mensajería directa 1:1 (jefe/oficina ↔ empleado o cliente), FUERA de las órdenes.
// ENTIDAD NUEVA a propósito, NO una generalización de Message: el ACL de Message es
// "eres parte de esta orden" y el de aquí es "estás en la lista de participantes".
// Mantenerlos separados físicamente evita que un WHERE olvidado filtre una
// conversación interna del personal a un cliente.
public class DirectConversation
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public required string CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public ICollection<DirectParticipant> Participants { get; set; } = [];
}

public class DirectParticipant
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public DirectConversation? Conversation { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
}

public class DirectMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public DirectConversation? Conversation { get; set; }
    public required string SenderUserId { get; set; }
    public ApplicationUser? Sender { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // hora del servidor
    public DateTime? CapturedAtUtc { get; set; }               // hora del dispositivo (cola)
    public string? SenderRole { get; set; }                    // congelado al enviar
    public string? IdempotencyKey { get; set; }                // cola offline
}
