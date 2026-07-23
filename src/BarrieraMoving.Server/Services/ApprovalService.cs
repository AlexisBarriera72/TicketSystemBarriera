using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Shared.Dtos;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

public sealed class ApprovalService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ISignatureService signatures,
    IPaperworkService paperwork) : IApprovalService
{
    public async Task<List<ApprovalItemDto>> GetPendingApprovalsAsync()
    {
        using var db = dbFactory.CreateDbContext();

        // 1) Firmas en estado Signed = esperando aprobación de oficina
        var pendingSigs = await signatures.GetPendingReviewAsync();
        var sigByOrder = pendingSigs
            .GroupBy(d => d.OrderId)
            .ToDictionary(g => g.Key, g => g.OrderBy(d => d.SignedAtUtc).First());

        // 2) Papeleo rechazado en órdenes aún no completadas
        var rejected = await db.PaperworkDocuments.Include(p => p.Order)
            .Where(p => p.Status == PaperworkStatus.Rejected && p.Order!.Status != OrderStatus.Completed)
            .ToListAsync();
        var rejectedByOrder = rejected
            .GroupBy(p => p.OrderId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.SlotKey).Distinct().ToList());

        var orderIds = sigByOrder.Keys.Union(rejectedByOrder.Keys).ToHashSet();
        if (orderIds.Count == 0) return [];

        var orders = await db.Orders.Include(o => o.AssignedDriver)
            .Where(o => orderIds.Contains(o.Id) && o.Status != OrderStatus.Completed)
            .ToListAsync();

        var slotLabels = paperwork.GetSlots().ToDictionary(s => s.Key, s => s.Label);
        string Label(string key) => slotLabels.TryGetValue(key, out var l) ? l : key;

        var result = new List<ApprovalItemDto>();
        foreach (var o in orders)
        {
            var pending = new List<string>();

            var signaturePending = sigByOrder.TryGetValue(o.Id, out var sig);
            if (signaturePending) pending.Add("Firma pendiente de aprobar");

            var rejectedLabels = new List<string>();
            if (rejectedByOrder.TryGetValue(o.Id, out var rejKeys))
            {
                rejectedLabels = rejKeys.Select(Label).ToList();
                foreach (var l in rejectedLabels) pending.Add($"Papeleo rechazado: {l}");
            }

            // Requeridos que faltan (no dupliques los ya listados como rechazados)
            var missing = await paperwork.GetMissingRequiredLabelsAsync(o.Id);
            foreach (var lbl in missing)
                if (!rejectedLabels.Contains(lbl))
                    pending.Add($"Falta papeleo: {lbl}");

            if (pending.Count == 0) continue;

            result.Add(new ApprovalItemDto(
                o.Id, o.Title,
                o.AssignedDriver?.DisplayName ?? o.AssignedDriver?.Email,
                o.Status.ToString(),
                signaturePending,
                sig?.SignedAtUtc,
                pending));
        }

        // Más urgente primero: firmas pendientes (las más antiguas arriba), luego el resto
        return result
            .OrderByDescending(r => r.SignaturePending)
            .ThenBy(r => r.SignedAtUtc ?? DateTime.MaxValue)
            .ThenBy(r => r.OrderId)
            .ToList();
    }

    public async Task<int> GetPendingCountAsync() => (await GetPendingApprovalsAsync()).Count;
}
