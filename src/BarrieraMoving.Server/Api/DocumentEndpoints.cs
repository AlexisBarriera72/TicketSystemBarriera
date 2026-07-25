using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;
using static BarrieraMoving.Server.Api.OrderAccess;

namespace BarrieraMoving.Server.Api;

// Documentos de firma: registros legales. Mismo control de acceso que las fotos
// (URLs no adivinables + ACL de la orden en cada petición) y SIN endpoints de
// borrado — nadie borra un documento firmado, tampoco un conductor.
public static class DocumentEndpoints
{
    private const long MaxSignatureBytes = 2 * 1024 * 1024;

    public static IEndpointRouteBuilder MapDocumentApi(this IEndpointRouteBuilder app)
    {
        // Ceremonia OFFLINE llegada desde la cola del móvil (multipart):
        // signature (PNG) + signerName + latitude/longitude/capturedAtUtc/idempotencyKey
        app.MapPost(ApiRoutes.Orders + "/{id:int}/signature/offline", async (int id, HttpRequest request,
            ClaimsPrincipal user, IOrderService orders, ISignatureService signatures) =>
        {
            var order = await orders.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();
            if (!CanAccess(user, order)) return ApiForbid();

            if (!request.HasFormContentType) return Results.BadRequest("Se esperaba multipart/form-data.");
            var form = await request.ReadFormAsync();
            var file = form.Files["signature"];
            var signerName = form["signerName"].FirstOrDefault()?.Trim();

            if (file is null || file.Length == 0) return Results.BadRequest("Falta la imagen de la firma.");
            if (file.Length > MaxSignatureBytes) return Results.BadRequest("La firma es demasiado grande.");
            if (string.IsNullOrWhiteSpace(signerName)) return Results.BadRequest("Falta el nombre del firmante.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var signaturePng = ms.ToArray();

            // Validar que es una imagen real (decodificable), sin re-codificarla
            using (var bitmap = SkiaSharp.SKBitmap.Decode(signaturePng))
            {
                if (bitmap is null) return Results.BadRequest("La firma no es una imagen válida.");
            }

            double? latitude = double.TryParse(form["latitude"], CultureInfo.InvariantCulture, out var lat) ? lat : null;
            double? longitude = double.TryParse(form["longitude"], CultureInfo.InvariantCulture, out var lng) ? lng : null;
            DateTime? capturedAt = DateTime.TryParse(form["capturedAtUtc"], CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var cap) ? cap : null;

            var (doc, error) = await signatures.CreateOfflineSignedAsync(
                id, user.FindFirstValue(ClaimTypes.NameIdentifier)!, signerName, signaturePng,
                latitude, longitude, capturedAt, form["idempotencyKey"].FirstOrDefault());

            return doc is null
                ? Results.BadRequest(error)
                : Results.Created($"{ApiRoutes.Documents}/{doc.Id}", doc.ToDto());
        }).RequireAuthorization(ApiAuth.EmployeePolicy).DisableAntiforgery();

        // Ruta ONLINE: crear sobre en el proveedor (adaptador real pendiente de nombrar)
        app.MapPost(ApiRoutes.Orders + "/{id:int}/signature/request", async (int id, CreateSignatureRequest request,
            ClaimsPrincipal user, IOrderService orders, ISignatureService signatures) =>
        {
            if (string.IsNullOrWhiteSpace(request.SignerName))
                return Results.BadRequest("Falta el nombre del firmante.");

            var order = await orders.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();
            if (!CanAccess(user, order)) return ApiForbid();

            var (doc, signingUrl, error) = await signatures.CreateProviderRequestAsync(
                id, user.FindFirstValue(ClaimTypes.NameIdentifier)!, request.SignerName.Trim());

            return doc is null
                ? Results.BadRequest(error)
                : Results.Ok(new CreateSignatureResponse(doc.Id, signingUrl!));
        }).RequireAuthorization(ApiAuth.EmployeePolicy);

        // Documentos de una orden (cliente incluido: es SU documento)
        app.MapGet(ApiRoutes.Orders + "/{id:int}/documents", async (int id,
            ClaimsPrincipal user, IOrderService orders, ISignatureService signatures) =>
        {
            var order = await orders.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();
            if (!CanAccess(user, order)) return ApiForbid();

            return Results.Ok((await signatures.GetForOrderAsync(id)).Select(d => d.ToDto()));
        }).RequireAuthorization(ApiAuth.Policy);

        // PDF firmado: cookie del dashboard O JWT del móvil; ACL de la orden SIEMPRE
        app.MapGet(ApiRoutes.Documents + "/{id:int}/pdf", async (int id,
            ClaimsPrincipal user, ISignatureService signatures, IPhotoStorage storage) =>
        {
            var doc = await signatures.GetWithOrderAsync(id);
            if (doc?.Order is null || doc.PdfPath is null) return Results.NotFound();
            if (!CanAccess(user, doc.Order)) return ApiForbid();

            var stream = storage.Open(doc.PdfPath);
            return stream is null
                ? Results.NotFound()
                : Results.File(stream, "application/pdf", $"Conformidad_Orden_{doc.OrderId}.pdf");
        }).RequireAuthorization(ApiAuth.PhotoPolicy);

        // Revisión de oficina: aprobar / rechazar con motivo (desbloquea Completed)
        app.MapPost(ApiRoutes.Documents + "/{id:int}/approve", async (int id,
            ClaimsPrincipal user, ISignatureService signatures) =>
        {
            var (ok, error) = await signatures.ApproveAsync(id, user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return ok ? Results.NoContent() : Results.BadRequest(error);
        }).RequireAuthorization(ApiAuth.StaffPolicy);

        app.MapPost(ApiRoutes.Documents + "/{id:int}/reject", async (int id, RejectDocumentRequest request,
            ClaimsPrincipal user, ISignatureService signatures) =>
        {
            var (ok, error) = await signatures.RejectAsync(id,
                user.FindFirstValue(ClaimTypes.NameIdentifier)!, request.Reason);
            return ok ? Results.NoContent() : Results.BadRequest(error);
        }).RequireAuthorization(ApiAuth.StaffPolicy);

        app.MapPost(ApiRoutes.Documents + "/{id:int}/resend-email", async (int id,
            ISignatureService signatures) =>
        {
            var (ok, error) = await signatures.ResendEmailAsync(id);
            return ok ? Results.NoContent() : Results.BadRequest(error);
        }).RequireAuthorization(ApiAuth.StaffPolicy);

        // Webhook del proveedor de firma. Anónimo pero verificado por HMAC:
        // un webhook sin verificar sería una puerta abierta a falsificar documentos.
        app.MapPost(ApiRoutes.EsignWebhook, async (HttpRequest request,
            IConfiguration config, ISignatureService signatures, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("EsignWebhook");
            var secret = config["ESign:WebhookSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                return Results.StatusCode(503); // webhook deshabilitado sin secreto configurado
            }

            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();

            // Firma HMAC-SHA256 del cuerpo con el secreto compartido, en hex.
            // (El formato exacto de cabecera se ajusta en el adaptador del proveedor real.)
            var received = request.Headers["X-ESign-Signature"].FirstOrDefault();
            var expected = Convert.ToHexString(HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body)));

            if (received is null || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(received.ToUpperInvariant()),
                    Encoding.UTF8.GetBytes(expected)))
            {
                logger.LogWarning("Webhook de firma RECHAZADO: firma HMAC inválida o ausente.");
                return Results.Unauthorized();
            }

            using var json = System.Text.Json.JsonDocument.Parse(body);
            var eventType = json.RootElement.TryGetProperty("event", out var ev) ? ev.GetString() : null;
            var envelopeId = json.RootElement.TryGetProperty("envelopeId", out var env) ? env.GetString() : null;

            if (eventType == "completed" && !string.IsNullOrEmpty(envelopeId))
            {
                await signatures.HandleEnvelopeCompletedAsync(envelopeId);
            }
            return Results.Ok(); // otros eventos: reconocidos e ignorados
        }).AllowAnonymous().DisableAntiforgery();

        return app;
    }
}
