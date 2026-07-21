using System.Globalization;
using System.Security.Claims;
using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;
using BarrieraMoving.Shared.Enums;
using static BarrieraMoving.Server.Api.OrderAccess;

namespace BarrieraMoving.Server.Api;

// Papeleo obligatorio de la orden. Mismo régimen que fotos y firmas: URLs no
// adivinables, ACL de la orden en cada petición y CERO endpoints de borrado.
public static class PaperworkEndpoints
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    public static IEndpointRouteBuilder MapPaperworkApi(this IEndpointRouteBuilder app)
    {
        // Estado de los slots de una orden (definición configurada + documento vigente)
        app.MapGet(ApiRoutes.Orders + "/{id:int}/paperwork", async (int id,
            ClaimsPrincipal user, IOrderService orders, IPaperworkService paperwork) =>
        {
            var order = await orders.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();
            if (!CanAccess(user, order)) return ApiForbid();

            var current = await paperwork.GetCurrentBySlotAsync(id);
            var state = paperwork.GetSlots().Select(slot => new PaperworkSlotStateDto(
                slot.Key, slot.Label, slot.Required,
                current.TryGetValue(slot.Key, out var doc) ? doc.ToDto() : null));
            return Results.Ok(state);
        }).RequireAuthorization(ApiAuth.Policy);

        // Adjuntar (multipart): file + slotKey + lat/lng/capturedAtUtc/idempotencyKey.
        // Imágenes → re-codificadas (EXIF fuera, 1600px/q75); PDFs → tal cual.
        app.MapPost(ApiRoutes.Orders + "/{id:int}/paperwork", async (int id, HttpRequest request,
            ClaimsPrincipal user, IOrderService orders, IPaperworkService paperwork) =>
        {
            var order = await orders.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();
            if (!CanAccess(user, order)) return ApiForbid();

            if (!request.HasFormContentType) return Results.BadRequest("Se esperaba multipart/form-data.");
            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            var slotKey = form["slotKey"].FirstOrDefault();
            if (file is null || file.Length == 0) return Results.BadRequest("Falta el archivo 'file'.");
            if (file.Length > MaxUploadBytes) return Results.BadRequest("Máximo 10 MB por documento.");
            if (string.IsNullOrWhiteSpace(slotKey)) return Results.BadRequest("Falta el slot de papeleo (slotKey).");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var raw = ms.ToArray();

            byte[] content;
            bool isPdf = raw.Length > 4 && raw[0] == '%' && raw[1] == 'P' && raw[2] == 'D' && raw[3] == 'F';
            if (isPdf)
            {
                content = raw; // los PDF no llevan EXIF; se conservan tal cual
            }
            else
            {
                ms.Position = 0;
                var processed = PhotoProcessor.ReencodeAsJpeg(ms);
                if (processed is null) return Results.BadRequest("El archivo no es una imagen ni un PDF válido.");
                content = processed.Value.Jpeg;
            }

            double? latitude = double.TryParse(form["latitude"], CultureInfo.InvariantCulture, out var lat) ? lat : null;
            double? longitude = double.TryParse(form["longitude"], CultureInfo.InvariantCulture, out var lng) ? lng : null;
            DateTime? capturedAt = DateTime.TryParse(form["capturedAtUtc"], CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var cap) ? cap : null;

            var (doc, error) = await paperwork.AttachAsync(id, slotKey!,
                user.FindFirstValue(ClaimTypes.NameIdentifier)!, content, isPdf,
                latitude, longitude, capturedAt, form["idempotencyKey"].FirstOrDefault());

            return doc is null
                ? Results.BadRequest(error)
                : Results.Created($"{ApiRoutes.Paperwork}/{doc.Id}/file", doc.ToDto());
        }).RequireAuthorization(ApiAuth.EmployeePolicy).DisableAntiforgery();

        // Servir el archivo (imagen o PDF): cookie del dashboard O JWT del móvil + ACL
        app.MapGet(ApiRoutes.Paperwork + "/{id:int}/file", async (int id,
            ClaimsPrincipal user, IPaperworkService paperwork, IPhotoStorage storage) =>
        {
            var doc = await paperwork.GetWithOrderAsync(id);
            if (doc?.Order is null) return Results.NotFound();
            if (!CanAccess(user, doc.Order)) return ApiForbid();

            var stream = storage.Open(doc.FilePath);
            return stream is null
                ? Results.NotFound()
                : Results.File(stream, doc.IsPdf ? "application/pdf" : "image/jpeg");
        }).RequireAuthorization(ApiAuth.PhotoPolicy);

        // Rechazo por slot (oficina): accionable, y en cascada invalida la firma
        // pendiente/firmada — el paquete que firmó el cliente ya no es el vigente
        app.MapPost(ApiRoutes.Paperwork + "/{id:int}/reject", async (int id, RejectDocumentRequest request,
            ClaimsPrincipal user, IPaperworkService paperwork) =>
        {
            var (ok, error) = await paperwork.RejectAsync(id,
                user.FindFirstValue(ClaimTypes.NameIdentifier)!, request.Reason);
            return ok ? Results.NoContent() : Results.BadRequest(error);
        }).RequireAuthorization(ApiAuth.StaffPolicy);

        return app;
    }
}
