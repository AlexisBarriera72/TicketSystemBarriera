namespace BarrieraMoving.Shared.Enums;

// Estado de un documento de papeleo adjuntado por el conductor:
// Attached → (Rejected con motivo | Replaced al subir uno nuevo al mismo slot).
// Nunca se borra nada: son registros, igual que las firmas.
public enum PaperworkStatus
{
    Attached,
    Rejected,
    Replaced,
}
