using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

// Definición de un slot de papeleo (Paperwork:Slots en appsettings — configurable,
// no cableado a 4; las etiquetas reales las decidirá el cliente final)
public record PaperworkSlot(string Key, string Label, bool Required);

public interface IPaperworkService
{
    IReadOnlyList<PaperworkSlot> GetSlots();

    // Documento VIGENTE (Attached) por slot para una orden
    Task<Dictionary<string, PaperworkDocument>> GetCurrentBySlotAsync(int orderId);

    // Todos los documentos de la orden (historial incluido, para la oficina)
    Task<List<PaperworkDocument>> GetForOrderAsync(int orderId);

    Task<PaperworkDocument?> GetWithOrderAsync(int documentId);

    // Adjunta al slot; el documento vigente anterior pasa a Replaced (nunca se borra)
    Task<(PaperworkDocument? Doc, string? Error)> AttachAsync(int orderId, string slotKey,
        string uploadedByUserId, byte[] content, bool isPdf, double? latitude, double? longitude,
        DateTime? capturedAtUtc, string? idempotencyKey);

    // Rechazo por slot (accionable). Si la orden tenía firma pendiente/firmada,
    // esa firma se invalida también: el paquete que firmó el cliente ya no es válido.
    Task<(bool Ok, string? Error)> RejectAsync(int documentId, string reviewerUserId, string reason);

    // Etiquetas de los slots obligatorios sin documento vigente (para mensajes de error)
    Task<List<string>> GetMissingRequiredLabelsAsync(int orderId);
}
