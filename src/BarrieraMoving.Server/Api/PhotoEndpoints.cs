using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Enums;
using static BarrieraMoving.Server.Api.OrderAccess;

namespace BarrieraMoving.Server.Api;

public static class PhotoEndpoints
{
    private const long MaxUploadBytes = 10 * 1024 * 1024; // red de seguridad; el móvil comprime a ~300 KB

    public static IEndpointRouteBuilder MapPhotoApi(this IEndpointRouteBuilder app)
    {
        // Subir foto al chat de la orden (multipart/form-data: file + campos opcionales
        // latitude/longitude/capturedAtUtc/idempotencyKey). Mismo ACL que los mensajes.
        app.MapPost(ApiRoutes.Orders + "/{id:int}/photos", async (int id, HttpRequest request,
            ClaimsPrincipal user, IOrderService orders, IPhotoStorage storage) =>
        {
            var order = await orders.GetOrderByIdAsync(id);
            if (order is null) return Results.NotFound();
            if (!CanAccess(user, order)) return ApiForbid();

            if (!request.HasFormContentType) return Results.BadRequest("Se esperaba multipart/form-data.");
            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            if (file is null || file.Length == 0) return Results.BadRequest("Falta el archivo 'file'.");
            if (file.Length > MaxUploadBytes) return Results.BadRequest("Máximo 10 MB por foto.");

            double? latitude = double.TryParse(form["latitude"], CultureInfo.InvariantCulture, out var lat) ? lat : null;
            double? longitude = double.TryParse(form["longitude"], CultureInfo.InvariantCulture, out var lng) ? lng : null;
            DateTime? capturedAt = DateTime.TryParse(form["capturedAtUtc"], CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var cap) ? cap : null;
            string? idempotencyKey = form["idempotencyKey"].FirstOrDefault();
            // Etapa: Recogida / Entrega. Si no se indica, foto suelta del chat.
            var stage = Enum.TryParse<PhotoStage>(form["stage"], ignoreCase: true, out var st)
                ? st : PhotoStage.General;

            // Reintento de la cola offline que sí llegó: devolver el original, no duplicar
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var existing = await orders.FindMessageByIdempotencyKeyAsync(idempotencyKey);
                if (existing is not null) return Results.Ok(existing.ToDto());
            }

            // Re-codificar SIEMPRE: valida que es imagen real y elimina todo EXIF
            await using var input = file.OpenReadStream();
            var processed = PhotoProcessor.ReencodeAsJpeg(input);
            if (processed is null) return Results.BadRequest("El archivo no es una imagen válida.");

            var (jpeg, thumb) = processed.Value;
            var name = Guid.NewGuid().ToString("N"); // nombres no adivinables
            var path = await storage.SaveAsync(id, $"{name}.jpg", jpeg);
            var thumbPath = await storage.SaveAsync(id, $"{name}_thumb.jpg", thumb);

            var message = new Message
            {
                OrderId = id,
                Content = stage switch
                {
                    PhotoStage.Pickup => "📷 Foto — Recogida",
                    PhotoStage.Delivery => "📷 Foto — Entrega",
                    _ => "📷 Foto",
                },
                Stage = stage,
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
                CreatedAt = DateTime.UtcNow,
                CapturedAtUtc = capturedAt,
                SenderRole = Roles.PrimaryRole(user),
                IsSystem = false,
                AttachmentPath = path,
                AttachmentThumbPath = thumbPath,
                Latitude = latitude,
                Longitude = longitude,
                IdempotencyKey = string.IsNullOrEmpty(idempotencyKey) ? null : idempotencyKey,
            };
            try
            {
                await orders.AddMessageAsync(message);
            }
            catch (DbUpdateException) when (!string.IsNullOrEmpty(idempotencyKey))
            {
                var existing = await orders.FindMessageByIdempotencyKeyAsync(idempotencyKey);
                if (existing is not null) return Results.Ok(existing.ToDto());
                throw;
            }
            return Results.Created($"{ApiRoutes.Photos}/{message.Id}", message.ToDto());
        }).RequireAuthorization(ApiAuth.Policy).DisableAntiforgery();

        // Servir foto / miniatura: cookie del dashboard (<img>) O JWT del móvil.
        // El ACL de la orden se revalida en CADA petición: adivinar la URL no sirve de nada.
        app.MapGet(ApiRoutes.Photos + "/{messageId:int}",
                (int messageId, ClaimsPrincipal user, IOrderService orders, IPhotoStorage storage) =>
                    ServeAsync(messageId, thumb: false, user, orders, storage))
            .RequireAuthorization(ApiAuth.PhotoPolicy);

        app.MapGet(ApiRoutes.Photos + "/{messageId:int}/thumb",
                (int messageId, ClaimsPrincipal user, IOrderService orders, IPhotoStorage storage) =>
                    ServeAsync(messageId, thumb: true, user, orders, storage))
            .RequireAuthorization(ApiAuth.PhotoPolicy);

        return app;
    }

    private static async Task<IResult> ServeAsync(int messageId, bool thumb,
        ClaimsPrincipal user, IOrderService orders, IPhotoStorage storage)
    {
        var message = await orders.GetMessageWithOrderAsync(messageId);
        if (message?.Order is null || message.AttachmentPath is null) return Results.NotFound();
        if (!CanAccess(user, message.Order)) return ApiForbid();

        var key = thumb ? (message.AttachmentThumbPath ?? message.AttachmentPath) : message.AttachmentPath;
        var stream = storage.Open(key);
        return stream is null ? Results.NotFound() : Results.File(stream, "image/jpeg");
    }
}
