namespace BarrieraMoving.Shared.Enums;

// Atención al cliente / reclamaciones. Sencillo a propósito: el ida y vuelta
// largo va por el chat de la orden o los mensajes directos; esto es el registro
// de la queja y la respuesta de la oficina.
public enum ComplaintStatus
{
    Open,       // recibida, sin revisar
    InReview,   // la oficina la está atendiendo
    Resolved,   // resuelta con respuesta
}
