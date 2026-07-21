using SQLite;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Mobile.Services;

// LA cola offline compartida (mensajes, fotos y fichajes) que las fases 3 y 4
// dejaron para aquí. Reglas:
// - persiste en SQLite: sobrevive a reinicios de la app
// - reintenta con backoff exponencial (30s → 10m, máx 10 intentos) y al recuperar red
// - NUNCA descarta nada en silencio: los fallos quedan visibles y reintentables
// - envía en orden de captura (FIFO), de uno en uno
public class OutboxService(IHttpClientFactory httpClientFactory, TokenStore tokenStore)
{
    public const int MaxItems = 200;
    public const int MaxAttempts = 10;

    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _flushGate = new(1, 1);
    private Timer? _nudgeTimer;
    private bool _hooked;

    // La UI (badge del header, chat, fichaje, página de cola) se suscribe aquí
    public event Action? Changed;

    private async Task<SQLiteAsyncConnection> DbAsync()
    {
        if (_db is null)
        {
            _db = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "outbox.db3"));
            await _db.CreateTableAsync<OutboxItem>();
            HookTriggers();
        }
        return _db;
    }

    private void HookTriggers()
    {
        if (_hooked) return;
        _hooked = true;
        // Al recuperar conectividad, vaciar la cola
        Connectivity.Current.ConnectivityChanged += (_, e) =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet) _ = FlushAsync();
        };
    }

    // --- Encolar (siempre se encola; si hay red, sale en <1 s) ---

    public Task<(bool Ok, string? Error)> EnqueueMessageAsync(int orderId, string text) =>
        EnqueueAsync(new OutboxItem { Kind = OutboxKind.Message, OrderId = orderId, Text = text.Trim() });

    public async Task<(bool Ok, string? Error)> EnqueuePhotoAsync(int orderId, byte[] jpeg, double? latitude, double? longitude)
    {
        var item = new OutboxItem { Kind = OutboxKind.Photo, OrderId = orderId, Latitude = latitude, Longitude = longitude };
        var dir = Path.Combine(FileSystem.AppDataDirectory, "outbox");
        Directory.CreateDirectory(dir);
        item.FilePath = Path.Combine(dir, $"{item.Id}.jpg");
        await File.WriteAllBytesAsync(item.FilePath, jpeg);
        return await EnqueueAsync(item);
    }

    // Ceremonia de firma offline: el PNG del lienzo se guarda en disco y viaja
    // por la misma cola FIFO — la firma de un cliente JAMÁS se pierde por falta de señal
    public async Task<(bool Ok, string? Error)> EnqueueSignatureAsync(int orderId, string signerName,
        byte[] signaturePng, double? latitude, double? longitude)
    {
        var item = new OutboxItem
        {
            Kind = OutboxKind.Signature,
            OrderId = orderId,
            Text = signerName,
            Latitude = latitude,
            Longitude = longitude,
        };
        var dir = Path.Combine(FileSystem.AppDataDirectory, "outbox");
        Directory.CreateDirectory(dir);
        item.FilePath = Path.Combine(dir, $"{item.Id}.png");
        await File.WriteAllBytesAsync(item.FilePath, signaturePng);
        return await EnqueueAsync(item);
    }

    // Papeleo obligatorio: misma mecánica que las fotos (JPEG comprimido en disco)
    public async Task<(bool Ok, string? Error)> EnqueuePaperworkAsync(int orderId, string slotKey,
        byte[] jpeg, double? latitude, double? longitude)
    {
        var item = new OutboxItem
        {
            Kind = OutboxKind.Paperwork,
            OrderId = orderId,
            Text = slotKey,
            Latitude = latitude,
            Longitude = longitude,
        };
        var dir = Path.Combine(FileSystem.AppDataDirectory, "outbox");
        Directory.CreateDirectory(dir);
        item.FilePath = Path.Combine(dir, $"{item.Id}.jpg");
        await File.WriteAllBytesAsync(item.FilePath, jpeg);
        return await EnqueueAsync(item);
    }

    public Task<(bool Ok, string? Error)> EnqueueClockAsync(bool clockIn, double? latitude, double? longitude) =>
        EnqueueAsync(new OutboxItem
        {
            Kind = clockIn ? OutboxKind.ClockIn : OutboxKind.ClockOut,
            Latitude = latitude,
            Longitude = longitude,
        });

    private async Task<(bool, string?)> EnqueueAsync(OutboxItem item)
    {
        var db = await DbAsync();
        var count = await db.Table<OutboxItem>().CountAsync();
        if (count >= MaxItems)
        {
            return (false, $"La cola está llena ({MaxItems}). Conéctate para vaciarla.");
        }
        await db.InsertAsync(item);
        RaiseChanged();
        _ = FlushAsync();
        return (true, null);
    }

    // --- Consultas para la UI ---

    public async Task<List<OutboxItem>> GetAllAsync()
    {
        var db = await DbAsync();
        return await db.Table<OutboxItem>().OrderBy(i => i.CapturedAtUtc).ToListAsync();
    }

    public async Task<List<OutboxItem>> GetForOrderAsync(int orderId)
    {
        var db = await DbAsync();
        return await db.Table<OutboxItem>()
            .Where(i => i.OrderId == orderId &&
                        (i.Kind == OutboxKind.Message || i.Kind == OutboxKind.Photo))
            .OrderBy(i => i.CapturedAtUtc)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        var db = await DbAsync();
        return await db.Table<OutboxItem>().CountAsync();
    }

    public async Task<bool> HasPendingClockAsync()
    {
        var db = await DbAsync();
        return await db.Table<OutboxItem>()
            .Where(i => i.Kind == OutboxKind.ClockIn || i.Kind == OutboxKind.ClockOut)
            .CountAsync() > 0;
    }

    // Reintento manual de un item en Failed
    public async Task RetryAsync(string id)
    {
        var db = await DbAsync();
        var item = await db.FindAsync<OutboxItem>(id);
        if (item is null) return;
        item.Status = OutboxStatus.Pending;
        item.NextAttemptUtc = DateTime.UtcNow;
        item.Attempts = 0;
        item.LastError = null;
        await db.UpdateAsync(item);
        RaiseChanged();
        _ = FlushAsync();
    }

    // Descarte EXPLÍCITO por el usuario (nunca automático)
    public async Task DiscardAsync(string id)
    {
        var db = await DbAsync();
        var item = await db.FindAsync<OutboxItem>(id);
        if (item is null) return;
        await db.DeleteAsync(item);
        DeleteStagedFile(item);
        RaiseChanged();
    }

    // --- Envío ---

    public async Task FlushAsync()
    {
        if (!await _flushGate.WaitAsync(0)) return; // ya hay un flush en marcha
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;
            if (await tokenStore.GetAccessTokenAsync() is null) return; // sin sesión: esperar al login

            var api = new ApiClient(httpClientFactory.CreateClient(nameof(ApiClient)));
            var db = await DbAsync();

            while (true)
            {
                var now = DateTime.UtcNow;
                var next = await db.Table<OutboxItem>()
                    .Where(i => i.Status != OutboxStatus.Failed)
                    .OrderBy(i => i.CapturedAtUtc)
                    .FirstOrDefaultAsync();
                if (next is null) break;
                if (next.NextAttemptUtc > now)
                {
                    ScheduleNudge(next.NextAttemptUtc - now);
                    break;
                }

                next.Status = OutboxStatus.Sending;
                await db.UpdateAsync(next);
                RaiseChanged();

                try
                {
                    var businessError = await SendAsync(api, next);
                    if (businessError is null)
                    {
                        await db.DeleteAsync(next); // entregado: fuera de la cola
                        DeleteStagedFile(next);
                    }
                    else
                    {
                        // El servidor lo rechazó (p. ej. fichaje ya auto-cerrado):
                        // reintentar no ayuda — visible para reintentar/descartar a mano
                        next.Status = OutboxStatus.Failed;
                        next.LastError = businessError;
                        await db.UpdateAsync(next);
                    }
                    RaiseChanged();
                }
                catch (Exception ex)
                {
                    // Sin red / timeout: backoff y a esperar
                    next.Attempts++;
                    next.LastError = ex is HttpRequestException ? "Sin conexión con el servidor" : ex.Message;
                    if (next.Attempts >= MaxAttempts)
                    {
                        next.Status = OutboxStatus.Failed; // visible; botón Reintentar lo reactiva
                    }
                    else
                    {
                        next.Status = OutboxStatus.Pending;
                        next.NextAttemptUtc = DateTime.UtcNow + Backoff(next.Attempts);
                        ScheduleNudge(Backoff(next.Attempts));
                    }
                    await db.UpdateAsync(next);
                    RaiseChanged();
                    break; // no seguir martilleando la red en este ciclo
                }
            }
        }
        finally
        {
            _flushGate.Release();
        }
    }

    // null = entregado; string = rechazo de negocio del servidor (no reintentabile)
    private static async Task<string?> SendAsync(ApiClient api, OutboxItem item)
    {
        switch (item.Kind)
        {
            case OutboxKind.Message:
                var (_, msgError) = await api.SendMessageAsync(item.OrderId, item.Text ?? "",
                    item.CapturedAtUtc, item.Id);
                return msgError;

            case OutboxKind.Photo:
                if (item.FilePath is null || !File.Exists(item.FilePath))
                {
                    return "El archivo de la foto ya no existe en el dispositivo.";
                }
                var jpeg = await File.ReadAllBytesAsync(item.FilePath);
                var (_, photoError) = await api.UploadPhotoAsync(item.OrderId, jpeg,
                    item.Latitude, item.Longitude, item.CapturedAtUtc, item.Id);
                return photoError;

            case OutboxKind.Signature:
                if (item.FilePath is null || !File.Exists(item.FilePath))
                {
                    return "La imagen de la firma ya no existe en el dispositivo.";
                }
                var png = await File.ReadAllBytesAsync(item.FilePath);
                var (_, sigError) = await api.SubmitOfflineSignatureAsync(item.OrderId, png,
                    item.Text ?? "", item.Latitude, item.Longitude, item.CapturedAtUtc, item.Id);
                return sigError;

            case OutboxKind.Paperwork:
                if (item.FilePath is null || !File.Exists(item.FilePath))
                {
                    return "El archivo del documento ya no existe en el dispositivo.";
                }
                var pw = await File.ReadAllBytesAsync(item.FilePath);
                var (_, pwError) = await api.UploadPaperworkAsync(item.OrderId, item.Text ?? "",
                    pw, item.Latitude, item.Longitude, item.CapturedAtUtc, item.Id);
                return pwError;

            case OutboxKind.ClockIn:
                var (_, inError) = await api.ClockInAsync(item.Latitude, item.Longitude,
                    item.CapturedAtUtc, item.Id);
                return inError;

            case OutboxKind.ClockOut:
                var (_, outError) = await api.ClockOutAsync(item.Latitude, item.Longitude,
                    item.CapturedAtUtc, item.Id);
                return outError;

            default:
                return "Tipo de envío desconocido.";
        }
    }

    private static TimeSpan Backoff(int attempts) => attempts switch
    {
        <= 1 => TimeSpan.FromSeconds(30),
        2 => TimeSpan.FromMinutes(1),
        3 => TimeSpan.FromMinutes(2),
        4 => TimeSpan.FromMinutes(5),
        _ => TimeSpan.FromMinutes(10),
    };

    private void ScheduleNudge(TimeSpan delay)
    {
        _nudgeTimer?.Dispose();
        _nudgeTimer = new Timer(_ => _ = FlushAsync(), null, delay, Timeout.InfiniteTimeSpan);
    }

    private static void DeleteStagedFile(OutboxItem item)
    {
        if (item.FilePath is not null)
        {
            try { File.Delete(item.FilePath); } catch (Exception) { }
        }
    }

    private void RaiseChanged() => Changed?.Invoke();
}
