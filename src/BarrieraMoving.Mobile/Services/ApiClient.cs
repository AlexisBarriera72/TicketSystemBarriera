using System.Net;
using System.Net.Http.Json;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Mobile.Services;

// Cliente tipado de /api/v1. Solo DTOs compartidos — nada de EF ni SQL (regla dura).
public class ApiClient(HttpClient http)
{
    // El servidor ya filtra por rol (Admin/Oficina todo, conductor lo suyo, cliente lo que creó)
    public async Task<List<OrderDto>> GetOrdersAsync() =>
        await http.GetFromJsonAsync<List<OrderDto>>(ApiRoutes.Orders, ApiJson.Options) ?? [];

    public async Task<OrderDto?> GetOrderAsync(int id)
    {
        var response = await http.GetAsync($"{ApiRoutes.Orders}/{id}");
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(ApiJson.Options);
    }

    // --- CHAT DE LA ORDEN ---

    public async Task<List<MessageDto>> GetOrderMessagesAsync(int orderId, int take = 50,
        int? beforeId = null, int? afterId = null)
    {
        var query = $"?take={take}";
        if (beforeId is not null) query += $"&beforeId={beforeId}";
        if (afterId is not null) query += $"&afterId={afterId}";
        return await http.GetFromJsonAsync<List<MessageDto>>(
            $"{ApiRoutes.Orders}/{orderId}/messages{query}", ApiJson.Options) ?? [];
    }

    public async Task<(MessageDto? Message, string? Error)> SendMessageAsync(int orderId, string content,
        DateTime? capturedAtUtc = null, string? idempotencyKey = null)
    {
        var response = await http.PostAsJsonAsync(
            $"{ApiRoutes.Orders}/{orderId}/messages",
            new CreateMessageRequest(content, capturedAtUtc, idempotencyKey), ApiJson.Options);

        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            return (null, "No tienes acceso a esta orden.");
        }
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return (null, "El mensaje no puede estar vacío.");
        }
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MessageDto>(ApiJson.Options), null);
    }

    // Foto al chat: multipart con el JPEG YA comprimido + GPS deliberado + metadatos de cola
    public async Task<(MessageDto? Message, string? Error)> UploadPhotoAsync(int orderId, byte[] jpeg,
        double? latitude, double? longitude, DateTime? capturedAtUtc = null, string? idempotencyKey = null)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(jpeg);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "file", "photo.jpg");
        if (latitude is not null)
            form.Add(new StringContent(latitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "latitude");
        if (longitude is not null)
            form.Add(new StringContent(longitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "longitude");
        if (capturedAtUtc is not null)
            form.Add(new StringContent(capturedAtUtc.Value.ToString("O")), "capturedAtUtc");
        if (!string.IsNullOrEmpty(idempotencyKey))
            form.Add(new StringContent(idempotencyKey), "idempotencyKey");

        var response = await http.PostAsync($"{ApiRoutes.Orders}/{orderId}/photos", form);
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            return (null, "No tienes acceso a esta orden.");
        }
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return (null, "El servidor rechazó la foto.");
        }
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MessageDto>(ApiJson.Options), null);
    }

    // Bytes de la foto (miniatura o completa) con el JWT — la WebView los muestra como data URI
    public async Task<byte[]?> GetPhotoAsync(int messageId, bool thumb)
    {
        var response = await http.GetAsync($"{ApiRoutes.Photos}/{messageId}{(thumb ? "/thumb" : "")}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync();
    }

    // --- MENSAJES DIRECTOS ---

    public async Task<List<DirectConversationDto>> GetConversationsAsync() =>
        await http.GetFromJsonAsync<List<DirectConversationDto>>(ApiRoutes.DirectMessages, ApiJson.Options) ?? [];

    public async Task<List<DirectMessageDto>> GetDirectMessagesAsync(int conversationId, int take = 50,
        int? beforeId = null, int? afterId = null)
    {
        var query = $"?take={take}";
        if (beforeId is not null) query += $"&beforeId={beforeId}";
        if (afterId is not null) query += $"&afterId={afterId}";
        return await http.GetFromJsonAsync<List<DirectMessageDto>>(
            $"{ApiRoutes.DirectMessages}/{conversationId}/messages{query}", ApiJson.Options) ?? [];
    }

    public async Task<(DirectMessageDto? Message, string? Error)> SendDirectMessageAsync(int conversationId,
        string content, DateTime? capturedAtUtc = null, string? idempotencyKey = null)
    {
        var response = await http.PostAsJsonAsync(
            $"{ApiRoutes.DirectMessages}/{conversationId}/messages",
            new SendDirectMessageRequest(content, capturedAtUtc, idempotencyKey), ApiJson.Options);
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            return (null, "No formas parte de esta conversación.");
        }
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return (null, "El mensaje no puede estar vacío.");
        }
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DirectMessageDto>(ApiJson.Options), null);
    }

    // --- PAPELEO OBLIGATORIO ---

    public async Task<List<PaperworkSlotStateDto>> GetOrderPaperworkAsync(int orderId) =>
        await http.GetFromJsonAsync<List<PaperworkSlotStateDto>>(
            $"{ApiRoutes.Orders}/{orderId}/paperwork", ApiJson.Options) ?? [];

    public async Task<(PaperworkDocumentDto? Doc, string? Error)> UploadPaperworkAsync(int orderId,
        string slotKey, byte[] jpeg, double? latitude, double? longitude,
        DateTime? capturedAtUtc = null, string? idempotencyKey = null)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(jpeg);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "file", "paperwork.jpg");
        form.Add(new StringContent(slotKey), "slotKey");
        if (latitude is not null)
            form.Add(new StringContent(latitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "latitude");
        if (longitude is not null)
            form.Add(new StringContent(longitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "longitude");
        if (capturedAtUtc is not null)
            form.Add(new StringContent(capturedAtUtc.Value.ToString("O")), "capturedAtUtc");
        if (!string.IsNullOrEmpty(idempotencyKey))
            form.Add(new StringContent(idempotencyKey), "idempotencyKey");

        var response = await http.PostAsync($"{ApiRoutes.Orders}/{orderId}/paperwork", form);
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            return (null, "No tienes acceso a esta orden.");
        }
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return (null, "El servidor rechazó el documento.");
        }
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PaperworkDocumentDto>(ApiJson.Options), null);
    }

    public async Task<byte[]?> GetPaperworkFileAsync(int documentId)
    {
        var response = await http.GetAsync($"{ApiRoutes.Paperwork}/{documentId}/file");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync();
    }

    // --- DOCUMENTOS DE FIRMA ---

    // Ceremonia offline: PNG del lienzo + nombre + GPS + metadatos de cola
    public async Task<(SignatureDocumentDto? Doc, string? Error)> SubmitOfflineSignatureAsync(int orderId,
        byte[] signaturePng, string signerName, double? latitude, double? longitude,
        DateTime? capturedAtUtc = null, string? idempotencyKey = null)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(signaturePng);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(file, "signature", "signature.png");
        form.Add(new StringContent(signerName), "signerName");
        if (latitude is not null)
            form.Add(new StringContent(latitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "latitude");
        if (longitude is not null)
            form.Add(new StringContent(longitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "longitude");
        if (capturedAtUtc is not null)
            form.Add(new StringContent(capturedAtUtc.Value.ToString("O")), "capturedAtUtc");
        if (!string.IsNullOrEmpty(idempotencyKey))
            form.Add(new StringContent(idempotencyKey), "idempotencyKey");

        var response = await http.PostAsync($"{ApiRoutes.Orders}/{orderId}/signature/offline", form);
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            return (null, "No tienes acceso a esta orden.");
        }
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return (null, "El servidor rechazó la firma.");
        }
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SignatureDocumentDto>(ApiJson.Options), null);
    }

    public async Task<List<SignatureDocumentDto>> GetOrderDocumentsAsync(int orderId) =>
        await http.GetFromJsonAsync<List<SignatureDocumentDto>>(
            $"{ApiRoutes.Orders}/{orderId}/documents", ApiJson.Options) ?? [];

    public async Task<byte[]?> GetDocumentPdfAsync(int documentId)
    {
        var response = await http.GetAsync($"{ApiRoutes.Documents}/{documentId}/pdf");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync();
    }

    // --- FICHAJE (la hora la pone el servidor; aquí solo van coordenadas opcionales) ---

    public async Task<TimeEntryDto?> GetCurrentTimeEntryAsync()
    {
        var response = await http.GetAsync($"{ApiRoutes.Time}/current");
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TimeEntryDto>(ApiJson.Options);
    }

    public Task<(TimeEntryDto? Entry, string? Error)> ClockInAsync(double? latitude, double? longitude,
        DateTime? capturedAtUtc = null, string? idempotencyKey = null) =>
        ClockAsync("clock-in", latitude, longitude, capturedAtUtc, idempotencyKey);

    public Task<(TimeEntryDto? Entry, string? Error)> ClockOutAsync(double? latitude, double? longitude,
        DateTime? capturedAtUtc = null, string? idempotencyKey = null) =>
        ClockAsync("clock-out", latitude, longitude, capturedAtUtc, idempotencyKey);

    private async Task<(TimeEntryDto?, string?)> ClockAsync(string action, double? latitude, double? longitude,
        DateTime? capturedAtUtc = null, string? idempotencyKey = null)
    {
        var response = await http.PostAsJsonAsync(
            $"{ApiRoutes.Time}/{action}",
            new ClockRequest(latitude, longitude, capturedAtUtc, idempotencyKey), ApiJson.Options);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var message = await response.Content.ReadFromJsonAsync<string>(ApiJson.Options);
            return (null, message ?? "Conflicto de fichaje.");
        }
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TimeEntryDto>(ApiJson.Options), null);
    }

    public async Task<List<TimeEntryDto>> GetTimeHistoryAsync(int days = 14) =>
        await http.GetFromJsonAsync<List<TimeEntryDto>>($"{ApiRoutes.Time}/history?days={days}", ApiJson.Options) ?? [];

    // Solo Admin/Oficina (el servidor devuelve 403 al resto)
    public async Task<List<TimeEntryDto>> GetActiveTeamEntriesAsync() =>
        await http.GetFromJsonAsync<List<TimeEntryDto>>($"{ApiRoutes.Time}/active", ApiJson.Options) ?? [];

    public async Task<List<TimeEntryDto>> GetTeamEntriesAsync(int days = 7) =>
        await http.GetFromJsonAsync<List<TimeEntryDto>>($"{ApiRoutes.Time}/entries?days={days}", ApiJson.Options) ?? [];
}
