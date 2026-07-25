using System.Security.Claims;
using BarrieraMoving.Server.Api;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Tests;

// CanAccess decide quién ve una orden (y con ella su chat, sus fotos y sus
// documentos firmados). Es LA regla de privacidad del sistema: si se relaja,
// un cliente podría ver la mudanza de otro.
public class OrderAccessTests
{
    private const string ClientId = "cliente-1";
    private const string DriverId = "conductor-1";
    private const string OtherId = "desconocido-9";

    private static ClaimsPrincipal User(string userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static Order TheOrder() => new()
    {
        Id = 1,
        Title = "Mudanza",
        Description = "…",
        AuthorId = ClientId,
        AssignedDriverId = DriverId,
    };

    [Fact]
    public void Admin_ve_cualquier_orden()
        => Assert.True(OrderAccess.CanAccess(User("quien-sea", Roles.Admin), TheOrder()));

    [Fact]
    public void Oficina_ve_cualquier_orden()
        => Assert.True(OrderAccess.CanAccess(User("quien-sea", Roles.Office), TheOrder()));

    [Fact]
    public void Conductor_asignado_ve_su_orden()
        => Assert.True(OrderAccess.CanAccess(User(DriverId, Roles.Driver), TheOrder()));

    [Fact]
    public void Cliente_autor_ve_su_orden()
        => Assert.True(OrderAccess.CanAccess(User(ClientId, Roles.Client), TheOrder()));

    // --- los casos que de verdad importan: los negativos ---

    [Fact]
    public void Conductor_NO_asignado_no_ve_la_orden()
        => Assert.False(OrderAccess.CanAccess(User("otro-conductor", Roles.Driver), TheOrder()));

    [Fact]
    public void Cliente_ajeno_no_ve_la_orden_de_otro()
        => Assert.False(OrderAccess.CanAccess(User("otro-cliente", Roles.Client), TheOrder()));

    [Fact]
    public void Usuario_sin_rol_ni_relacion_no_ve_nada()
        => Assert.False(OrderAccess.CanAccess(User(OtherId), TheOrder()));

    [Fact]
    public void Un_conductor_no_hereda_acceso_por_ser_autor_de_otra_orden()
    {
        var order = TheOrder();
        order.AssignedDriverId = "conductor-distinto";
        Assert.False(OrderAccess.CanAccess(User(DriverId, Roles.Driver), order));
    }

    [Fact]
    public void Orden_sin_conductor_sigue_siendo_visible_para_su_autor()
    {
        var order = TheOrder();
        order.AssignedDriverId = null;
        Assert.True(OrderAccess.CanAccess(User(ClientId, Roles.Client), order));
        Assert.False(OrderAccess.CanAccess(User(DriverId, Roles.Driver), order));
    }
}
