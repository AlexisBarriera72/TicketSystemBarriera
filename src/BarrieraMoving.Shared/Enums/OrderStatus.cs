namespace BarrieraMoving.Shared.Enums;

// Flujo de una mudanza: Requested → Assigned → EnRoute → InProgress → PendingSignature → Completed
public enum OrderStatus
{
    Requested,
    Assigned,
    EnRoute,
    InProgress,
    PendingSignature,
    Completed,
}
