using BarrieraMoving.Server.Models;
using BarrieraMoving.Server.Services;

namespace BarrieraMoving.Server.Tests;

// Consulta de clientes SIN cuenta: código de referencia + teléfono. Es una
// puerta ANÓNIMA a datos de un cliente, así que lo que más importa aquí son los
// casos negativos: con el código de otro, o con un teléfono que no casa, no se
// puede ver nada.
public class GuestLookupTests
{
    private static async Task<(GuestLookupService Svc, TestDb Db, string Code)> SeedAsync(
        string phone = "787-555-1234")
    {
        var db = new TestDb();
        var svc = new GuestLookupService(db.Factory);
        var code = await svc.GenerateReferenceCodeAsync();

        using (var ctx = db.NewContext())
        {
            ctx.QuoteRequests.Add(new QuoteRequest
            {
                ReferenceCode = code,
                Name = "María Cliente",
                Phone = phone,
                ServiceType = "Mudanza residencial",
            });
            await ctx.SaveChangesAsync();
        }
        return (svc, db, code);
    }

    [Fact]
    public async Task El_codigo_correcto_con_su_telefono_devuelve_la_solicitud()
    {
        var (svc, db, code) = await SeedAsync();
        using var _ = db;

        var result = await svc.LookupAsync(code, "787-555-1234");

        Assert.NotNull(result);
        Assert.Equal("María Cliente", result!.CustomerName);
        Assert.False(result.Converted); // aún no hay orden
    }

    [Theory]
    [InlineData("7875551234")]        // sin separadores
    [InlineData("(787) 555 1234")]    // con paréntesis
    [InlineData("+1 787-555-1234")]   // con prefijo de país
    [InlineData("787.555.1234")]      // con puntos
    public async Task Acepta_el_telefono_en_cualquier_formato(string typed)
    {
        var (svc, db, code) = await SeedAsync();
        using var _ = db;
        Assert.NotNull(await svc.LookupAsync(code, typed));
    }

    [Theory]
    [InlineData("tc-")]               // minúsculas + prefijo
    [InlineData("")]                  // sin prefijo
    public async Task Acepta_el_codigo_en_cualquier_formato(string prefix)
    {
        var (svc, db, code) = await SeedAsync();
        using var _ = db;
        var bare = code.Replace("TC-", "");
        var typed = prefix + (prefix == "tc-" ? bare.ToLowerInvariant() : bare);
        Assert.NotNull(await svc.LookupAsync(typed, "787-555-1234"));
    }

    // --- los negativos: lo que protege los datos del cliente ---

    [Fact]
    public async Task Con_el_telefono_equivocado_NO_devuelve_nada()
    {
        var (svc, db, code) = await SeedAsync();
        using var _ = db;
        Assert.Null(await svc.LookupAsync(code, "787-999-0000"));
    }

    [Fact]
    public async Task Un_codigo_inexistente_NO_devuelve_nada()
    {
        var (svc, db, _) = await SeedAsync();
        using var __ = db;
        Assert.Null(await svc.LookupAsync("TC-ZZZZZZ", "787-555-1234"));
    }

    [Fact]
    public async Task No_se_puede_consultar_solo_con_el_codigo()
    {
        var (svc, db, code) = await SeedAsync();
        using var _ = db;
        Assert.Null(await svc.LookupAsync(code, ""));
    }

    [Fact]
    public async Task Los_codigos_generados_son_distintos_entre_si()
    {
        using var db = new TestDb();
        var svc = new GuestLookupService(db.Factory);

        var codes = new HashSet<string>();
        for (var i = 0; i < 50; i++) codes.Add(await svc.GenerateReferenceCodeAsync());

        Assert.Equal(50, codes.Count);
    }

    [Fact]
    public void El_codigo_evita_caracteres_ambiguos()
    {
        using var db = new TestDb();
        var svc = new GuestLookupService(db.Factory);

        for (var i = 0; i < 30; i++)
        {
            var code = svc.GenerateReferenceCodeAsync().Result.Replace("TC-", "");
            // 0/O y 1/I/L se confunden al dictarlos por teléfono
            Assert.DoesNotContain(code, c => c is '0' or 'O' or '1' or 'I' or 'L');
        }
    }
}
