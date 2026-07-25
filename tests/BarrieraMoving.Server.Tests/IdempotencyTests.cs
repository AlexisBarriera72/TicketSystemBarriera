using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BarrieraMoving.Server.Services;

namespace BarrieraMoving.Server.Tests;

// La app móvil tiene una cola offline que REINTENTA. Si el servidor recibió el
// envío pero la respuesta se perdió, el reintento llega con la misma clave de
// idempotencia y NO debe duplicar: ni un mensaje repetido en el chat, ni —lo
// grave— un fichaje de más en la nómina.
public class IdempotencyTests
{
    private const string Driver = "conductor-1";

    private static TimeService BuildTime(TestDb db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Time:MaxShiftHours"] = "12" })
            .Build();
        return new TimeService(db.Factory, config);
    }

    [Fact]
    public async Task Reenviar_el_mismo_fichaje_de_entrada_no_crea_dos_jornadas()
    {
        using var db = new TestDb();
        var time = BuildTime(db);
        const string key = "11111111-1111-1111-1111-111111111111";

        var (first, err1) = await time.ClockInAsync(Driver, null, null, idempotencyKey: key);
        var (second, err2) = await time.ClockInAsync(Driver, null, null, idempotencyKey: key);

        Assert.Null(err1);
        Assert.NotNull(first);
        // El reintento devuelve el MISMO fichaje, no uno nuevo ni un error
        Assert.Null(err2);
        Assert.NotNull(second);
        Assert.Equal(first!.Id, second!.Id);

        using var ctx = db.NewContext();
        Assert.Equal(1, await ctx.TimeEntries.CountAsync());
    }

    [Fact]
    public async Task Reenviar_el_mismo_fichaje_de_salida_no_cierra_dos_veces()
    {
        using var db = new TestDb();
        var time = BuildTime(db);
        const string key = "22222222-2222-2222-2222-222222222222";

        await time.ClockInAsync(Driver, null, null);
        var (first, _) = await time.ClockOutAsync(Driver, null, null, idempotencyKey: key);
        var (second, err) = await time.ClockOutAsync(Driver, null, null, idempotencyKey: key);

        Assert.NotNull(first);
        Assert.Null(err);
        Assert.NotNull(second);
        Assert.Equal(first!.Id, second!.Id);
        Assert.Equal(first.ClockOutUtc, second.ClockOutUtc); // la hora no se reescribe
    }

    [Fact]
    public async Task Claves_distintas_si_producen_fichajes_distintos()
    {
        using var db = new TestDb();
        var time = BuildTime(db);

        await time.ClockInAsync(Driver, null, null, idempotencyKey: "clave-A");
        await time.ClockOutAsync(Driver, null, null, idempotencyKey: "clave-B");
        await time.ClockInAsync(Driver, null, null, idempotencyKey: "clave-C");

        using var ctx = db.NewContext();
        Assert.Equal(2, await ctx.TimeEntries.CountAsync());
    }

    [Fact]
    public async Task Un_mensaje_reenviado_con_la_misma_clave_no_se_duplica()
    {
        using var db = new TestDb();
        var dm = new DirectMessageService(db.Factory, new NoNotify());

        // Conversación entre oficina y conductor
        int convId;
        using (var ctx = db.NewContext())
        {
            var conv = new BarrieraMoving.Server.Models.DirectConversation
            {
                CreatedByUserId = "oficina-1",
                Participants =
                {
                    new() { UserId = "oficina-1" },
                    new() { UserId = Driver },
                },
            };
            ctx.DirectConversations.Add(conv);
            await ctx.SaveChangesAsync();
            convId = conv.Id;
        }

        const string key = "33333333-3333-3333-3333-333333333333";
        var (m1, e1) = await dm.SendAsync(convId, Driver, "Driver", "Voy en camino", null, key);
        var (m2, e2) = await dm.SendAsync(convId, Driver, "Driver", "Voy en camino", null, key);

        Assert.Null(e1);
        Assert.Null(e2);
        Assert.Equal(m1!.Id, m2!.Id);

        using var check = db.NewContext();
        Assert.Equal(1, await check.DirectMessages.CountAsync());
    }

    private sealed class NoNotify : INotificationService
    {
        public Task NotifyOrderMessageAsync(int orderId, string s, string n, string p) => Task.CompletedTask;
        public Task NotifyDirectMessageAsync(int conversationId, string s, string n, string p) => Task.CompletedTask;
        public Task NotifyComplaintResponseAsync(int complaintId) => Task.CompletedTask;
        public Task NotifyOrderStatusAsync(int orderId, string? u, BarrieraMoving.Shared.Enums.OrderStatus s) => Task.CompletedTask;
    }
}
