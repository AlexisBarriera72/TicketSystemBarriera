using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

public class PaperworkService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IConfiguration config,
    IPhotoStorage storage) : IPaperworkService
{
    private IReadOnlyList<PaperworkSlot>? _slots;

    public IReadOnlyList<PaperworkSlot> GetSlots()
    {
        if (_slots is null)
        {
            var list = new List<PaperworkSlot>();
            foreach (var section in config.GetSection("Paperwork:Slots").GetChildren())
            {
                var key = section["Key"];
                var label = section["Label"];
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(label)) continue;
                list.Add(new PaperworkSlot(key, label, bool.TryParse(section["Required"], out var req) && req));
            }
            _slots = list;
        }
        return _slots;
    }

    public async Task<Dictionary<string, PaperworkDocument>> GetCurrentBySlotAsync(int orderId)
    {
        using var context = dbFactory.CreateDbContext();
        var docs = await context.PaperworkDocuments
            .Include(p => p.ReviewedBy)
            .Where(p => p.OrderId == orderId && p.Status != PaperworkStatus.Replaced)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync();
        // Vigente = el más reciente no reemplazado de cada slot (Attached o Rejected)
        return docs.GroupBy(p => p.SlotKey).ToDictionary(g => g.Key, g => g.First());
    }

    public async Task<List<PaperworkDocument>> GetForOrderAsync(int orderId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.PaperworkDocuments
            .Include(p => p.ReviewedBy)
            .Where(p => p.OrderId == orderId)
            .OrderBy(p => p.SlotKey).ThenByDescending(p => p.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<PaperworkDocument?> GetWithOrderAsync(int documentId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.PaperworkDocuments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == documentId);
    }

    public async Task<(PaperworkDocument?, string?)> AttachAsync(int orderId, string slotKey,
        string uploadedByUserId, byte[] content, bool isPdf, double? latitude, double? longitude,
        DateTime? capturedAtUtc, string? idempotencyKey)
    {
        var slot = GetSlots().FirstOrDefault(s => s.Key == slotKey);
        if (slot is null) return (null, $"Slot de papeleo desconocido: '{slotKey}'.");

        using var context = dbFactory.CreateDbContext();

        // Reintento de la cola offline que ya llegó
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await context.PaperworkDocuments
                .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);
            if (existing is not null) return (existing, null);
        }

        // Guardar archivo: imágenes YA llegan re-codificadas (PhotoProcessor en el
        // endpoint); los PDF se guardan tal cual
        var name = Guid.NewGuid().ToString("N");
        var path = await storage.SaveAsync(orderId, isPdf ? $"pw_{name}.pdf" : $"pw_{name}.jpg", content);

        var doc = new PaperworkDocument
        {
            OrderId = orderId,
            SlotKey = slotKey,
            UploadedByUserId = uploadedByUserId,
            FilePath = path,
            IsPdf = isPdf,
            ContentHash = Convert.ToHexString(SHA256.HashData(content)),
            CapturedAtUtc = capturedAtUtc,
            Latitude = latitude,
            Longitude = longitude,
            IdempotencyKey = string.IsNullOrEmpty(idempotencyKey) ? null : idempotencyKey,
        };

        // El documento vigente anterior del slot queda como Replaced (historial intacto)
        var previous = await context.PaperworkDocuments
            .Where(p => p.OrderId == orderId && p.SlotKey == slotKey && p.Status != PaperworkStatus.Replaced)
            .ToListAsync();
        foreach (var old in previous) old.Status = PaperworkStatus.Replaced;

        context.PaperworkDocuments.Add(doc);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException) when (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await context.PaperworkDocuments
                .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);
            if (existing is not null) return (existing, null);
            throw;
        }
        return (doc, null);
    }

    public async Task<(bool, string?)> RejectAsync(int documentId, string reviewerUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "El rechazo necesita un motivo: el conductor debe saber qué rehacer.");

        using var context = dbFactory.CreateDbContext();
        var doc = await context.PaperworkDocuments.FindAsync(documentId);
        if (doc is null) return (false, "El documento no existe.");
        if (doc.Status != PaperworkStatus.Attached)
            return (false, "Solo se puede rechazar un documento vigente.");

        doc.Status = PaperworkStatus.Rejected;
        doc.RejectReason = reason.Trim();
        doc.ReviewedByUserId = reviewerUserId;
        doc.ReviewedAtUtc = DateTime.UtcNow;

        // SECUENCIA FIRMA-PAPELEO: el cliente firmó el paquete COMPLETO. Si un
        // papel se rechaza, ese paquete ya no es válido → la firma pendiente o
        // firmada se invalida también y habrá que re-firmar tras reemplazarlo.
        var label = GetSlots().FirstOrDefault(s => s.Key == doc.SlotKey)?.Label ?? doc.SlotKey;
        var affectedSignatures = await context.SignatureDocuments
            .Where(s => s.OrderId == doc.OrderId &&
                        (s.Status == SignatureDocStatus.AwaitingSignature || s.Status == SignatureDocStatus.Signed))
            .ToListAsync();
        foreach (var sig in affectedSignatures)
        {
            sig.Status = SignatureDocStatus.Rejected;
            sig.RejectReason = $"Documento '{label}' rechazado por la oficina: {reason.Trim()} — reemplazarlo y repetir la firma.";
            sig.ReviewedByUserId = reviewerUserId;
            sig.ReviewedAtUtc = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<string>> GetMissingRequiredLabelsAsync(int orderId)
    {
        var current = await GetCurrentBySlotAsync(orderId);
        return GetSlots()
            .Where(s => s.Required &&
                        (!current.TryGetValue(s.Key, out var doc) || doc.Status != PaperworkStatus.Attached))
            .Select(s => s.Label)
            .ToList();
    }
}
