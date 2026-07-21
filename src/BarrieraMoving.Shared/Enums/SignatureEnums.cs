namespace BarrieraMoving.Shared.Enums;

// Ciclo de vida de un documento de firma:
// AwaitingSignature → Signed → Approved | Rejected (con motivo).
// La orden solo puede llegar a Completed con un documento Approved.
public enum SignatureDocStatus
{
    AwaitingSignature,
    Signed,
    Approved,
    Rejected,
}

// Estado del envío de la copia firmada al cliente. Un documento legal sin enviar
// debe ser un ERROR VISIBLE, nunca un silencio.
public enum EmailDeliveryStatus
{
    NotSent,
    Sent,
    Failed,
    NotConfigured, // aún no hay servicio de correo configurado en el servidor
}
