using BarrieraMoving.Server.Models;
using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Tests;

// EL GATE LEGAL: una orden no puede pasar a Completed sin (a) todo el papeleo
// obligatorio adjunto y (b) un documento firmado por el cliente Y aprobado por
// la oficina. Es la protección de la empresa ante una reclamación posterior, así
// que ni siquiera bypassValidation debe saltárselo.
public class CompletionGateTests
{
    private static (OrderService Orders, TestDb Db) Build(params PaperworkSlot[] slots)
    {
        var db = new TestDb();
        var paperwork = new FakePaperwork(slots);
        var orders = new OrderService(db.Factory, paperwork, new NoNotifications());
        return (orders, db);
    }

    private static async Task<int> SeedOrderAsync(TestDb db, OrderStatus status)
    {
        using var ctx = db.NewContext();
        var order = new Order
        {
            Title = "Mudanza",
            Description = "…",
            AuthorId = "cliente-1",
            CategoryId = TestDb.SeedCategoryId,
            AssignedDriverId = "conductor-1",
            Status = status,
        };
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();
        return order.Id;
    }

    [Fact]
    public async Task No_completa_sin_documento_firmado()
    {
        var (orders, db) = Build();
        using var _ = db;
        var id = await SeedOrderAsync(db, OrderStatus.PendingSignature);

        var (ok, error) = await orders.UpdateOrderStatusAsync(id, OrderStatus.Completed);

        Assert.False(ok);
        Assert.Contains("firmado", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task No_completa_si_la_firma_esta_solo_firmada_pero_no_aprobada()
    {
        var (orders, db) = Build();
        using var _ = db;
        var id = await SeedOrderAsync(db, OrderStatus.PendingSignature);

        using (var ctx = db.NewContext())
        {
            ctx.SignatureDocuments.Add(new SignatureDocument
            {
                OrderId = id,
                RequestedByUserId = "conductor-1",
                Status = SignatureDocStatus.Signed, // falta la aprobación de oficina
            });
            await ctx.SaveChangesAsync();
        }

        var (ok, _) = await orders.UpdateOrderStatusAsync(id, OrderStatus.Completed);
        Assert.False(ok);
    }

    [Fact]
    public async Task No_completa_si_falta_papeleo_obligatorio()
    {
        var (orders, db) = Build(new PaperworkSlot("contrato", "Contrato", true));
        using var _ = db;
        var id = await SeedOrderAsync(db, OrderStatus.PendingSignature);

        using (var ctx = db.NewContext())
        {
            ctx.SignatureDocuments.Add(new SignatureDocument
            {
                OrderId = id,
                RequestedByUserId = "conductor-1",
                Status = SignatureDocStatus.Approved,
            });
            await ctx.SaveChangesAsync();
        }

        var (ok, error) = await orders.UpdateOrderStatusAsync(id, OrderStatus.Completed);

        Assert.False(ok);
        Assert.Contains("papeleo", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Completa_cuando_hay_firma_aprobada_y_papeleo_completo()
    {
        var (orders, db) = Build();
        using var _ = db;
        var id = await SeedOrderAsync(db, OrderStatus.PendingSignature);

        using (var ctx = db.NewContext())
        {
            ctx.SignatureDocuments.Add(new SignatureDocument
            {
                OrderId = id,
                RequestedByUserId = "conductor-1",
                Status = SignatureDocStatus.Approved,
            });
            await ctx.SaveChangesAsync();
        }

        var (ok, error) = await orders.UpdateOrderStatusAsync(id, OrderStatus.Completed);

        Assert.True(ok, error);
        using var check = db.NewContext();
        Assert.Equal(OrderStatus.Completed, (await check.Orders.FindAsync(id))!.Status);
    }

    [Fact]
    public async Task El_gate_NO_se_salta_con_bypassValidation()
    {
        var (orders, db) = Build();
        using var _ = db;
        // bypassValidation permite saltarse el ORDEN de los estados…
        var id = await SeedOrderAsync(db, OrderStatus.Requested);

        var (ok, error) = await orders.UpdateOrderStatusAsync(
            id, OrderStatus.Completed, bypassValidation: true);

        // …pero nunca el requisito legal de firma aprobada.
        Assert.False(ok);
        Assert.Contains("firmado", error, StringComparison.OrdinalIgnoreCase);
    }

    // --- dobles de prueba ---

    private sealed class FakePaperwork(PaperworkSlot[] slots) : IPaperworkService
    {
        public IReadOnlyList<PaperworkSlot> GetSlots() => slots;

        // Sin documentos adjuntos: faltan todos los obligatorios
        public Task<List<string>> GetMissingRequiredLabelsAsync(int orderId) =>
            Task.FromResult(slots.Where(s => s.Required).Select(s => s.Label).ToList());

        public Task<Dictionary<string, PaperworkDocument>> GetCurrentBySlotAsync(int orderId) => Task.FromResult(new Dictionary<string, PaperworkDocument>());
        public Task<List<PaperworkDocument>> GetForOrderAsync(int orderId) => Task.FromResult(new List<PaperworkDocument>());
        public Task<PaperworkDocument?> GetWithOrderAsync(int documentId) => Task.FromResult<PaperworkDocument?>(null);
        public Task<(PaperworkDocument? Doc, string? Error)> AttachAsync(int orderId, string slotKey,
            string uploadedByUserId, byte[] content, bool isPdf, double? latitude, double? longitude,
            DateTime? capturedAtUtc, string? idempotencyKey) => Task.FromResult<(PaperworkDocument?, string?)>((null, "no usado"));
        public Task<(bool Ok, string? Error)> RejectAsync(int documentId, string reviewerUserId, string reason)
            => Task.FromResult((false, (string?)"no usado"));
    }

    private sealed class NoNotifications : INotificationService
    {
        public Task NotifyOrderMessageAsync(int orderId, string senderUserId, string senderName, string preview) => Task.CompletedTask;
        public Task NotifyDirectMessageAsync(int conversationId, string senderUserId, string senderName, string preview) => Task.CompletedTask;
        public Task NotifyComplaintResponseAsync(int complaintId) => Task.CompletedTask;
        public Task NotifyOrderStatusAsync(int orderId, string? performerUserId, OrderStatus newStatus) => Task.CompletedTask;
    }
}
