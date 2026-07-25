using Microsoft.Extensions.Configuration;
using BarrieraMoving.Server.Services;

namespace BarrieraMoving.Server.Tests;

// Fichajes = datos de NÓMINA. Las reglas: la hora la pone siempre el servidor,
// solo puede haber una jornada abierta por empleado, y si alguien olvida marcar
// la salida la jornada se auto-cierra a Time:MaxShiftHours con una marca visible
// (AutoClosed) para que la oficina la revise en vez de pagar 14 horas de más.
public class TimeServiceTests
{
    private const string Driver = "conductor-1";

    private static TimeService Build(TestDb db, double maxShiftHours = 12)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Time:MaxShiftHours"] = maxShiftHours.ToString(),
            })
            .Build();
        return new TimeService(db.Factory, config);
    }

    [Fact]
    public async Task Fichar_entrada_abre_una_jornada()
    {
        using var db = new TestDb();
        var time = Build(db);

        var (entry, error) = await time.ClockInAsync(Driver, null, null);

        Assert.Null(error);
        Assert.NotNull(entry);
        Assert.Null(entry!.ClockOutUtc);
        Assert.NotNull(await time.GetOpenEntryAsync(Driver));
    }

    [Fact]
    public async Task La_hora_la_pone_el_SERVIDOR_no_el_dispositivo()
    {
        using var db = new TestDb();
        var time = Build(db);

        // El teléfono dice que fichó hace tres días (reloj mal, o cola offline)
        var deviceTime = DateTime.UtcNow.AddDays(-3);
        var (entry, _) = await time.ClockInAsync(Driver, null, null, capturedAtUtc: deviceTime);

        Assert.NotNull(entry);
        // La hora oficial es la del servidor, no la del aparato
        Assert.True((DateTime.UtcNow - entry!.ClockInUtc).TotalMinutes < 1);
        Assert.Equal(deviceTime, entry.ClockInCapturedAtUtc); // el dato del móvil queda como metadato
    }

    [Fact]
    public async Task No_permite_dos_jornadas_abiertas_a_la_vez()
    {
        using var db = new TestDb();
        var time = Build(db);

        await time.ClockInAsync(Driver, null, null);
        var (second, error) = await time.ClockInAsync(Driver, null, null);

        Assert.Null(second);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Fichar_salida_cierra_la_jornada_y_calcula_horas()
    {
        using var db = new TestDb();
        var time = Build(db);
        await time.ClockInAsync(Driver, null, null);

        var (closed, error) = await time.ClockOutAsync(Driver, null, null);

        Assert.Null(error);
        Assert.NotNull(closed!.ClockOutUtc);
        Assert.False(closed.AutoClosed);
        Assert.True(closed.ClockOutUtc >= closed.ClockInUtc);
        Assert.Null(await time.GetOpenEntryAsync(Driver)); // ya no hay jornada abierta
    }

    [Fact]
    public async Task No_permite_fichar_salida_sin_haber_entrado()
    {
        using var db = new TestDb();
        var time = Build(db);

        var (entry, error) = await time.ClockOutAsync(Driver, null, null);

        Assert.Null(entry);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Una_jornada_olvidada_se_auto_cierra_al_maximo_y_queda_marcada()
    {
        using var db = new TestDb();
        var time = Build(db, maxShiftHours: 12);

        // Jornada de ayer que nadie cerró
        using (var ctx = db.NewContext())
        {
            ctx.TimeEntries.Add(new BarrieraMoving.Server.Models.TimeEntry
            {
                UserId = Driver,
                ClockInUtc = DateTime.UtcNow.AddDays(-1),
            });
            await ctx.SaveChangesAsync();
        }

        // Al fichar hoy, la anterior se cierra sola
        var (entry, error) = await time.ClockInAsync(Driver, null, null);
        Assert.Null(error);
        Assert.NotNull(entry);

        using var check = db.NewContext();
        var auto = check.TimeEntries.Single(t => t.Id != entry!.Id);
        Assert.True(auto.AutoClosed);
        // No se inventan horas reales: exactamente el máximo configurado
        Assert.Equal(12, (auto.ClockOutUtc!.Value - auto.ClockInUtc).TotalHours, precision: 2);
    }

    [Fact]
    public async Task Empleados_distintos_pueden_tener_jornadas_abiertas_a_la_vez()
    {
        using var db = new TestDb();
        var time = Build(db);

        var (a, errA) = await time.ClockInAsync("conductor-A", null, null);
        var (b, errB) = await time.ClockInAsync("conductor-B", null, null);

        Assert.Null(errA);
        Assert.Null(errB);
        Assert.NotNull(a);
        Assert.NotNull(b);
    }
}
