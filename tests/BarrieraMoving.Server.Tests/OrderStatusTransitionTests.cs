using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Tests;

// El flujo de una mudanza es Requested -> Assigned -> EnRoute -> InProgress ->
// PendingSignature -> Completed. Estas reglas protegen la facturación y el
// historial: un salto silencioso rompe la trazabilidad de la orden.
public class OrderStatusTransitionTests
{
    [Theory]
    [InlineData(OrderStatus.Requested, OrderStatus.Assigned)]
    [InlineData(OrderStatus.Assigned, OrderStatus.EnRoute)]
    [InlineData(OrderStatus.EnRoute, OrderStatus.InProgress)]
    [InlineData(OrderStatus.InProgress, OrderStatus.PendingSignature)]
    [InlineData(OrderStatus.PendingSignature, OrderStatus.Completed)]
    public void Permite_el_paso_al_siguiente_estado(OrderStatus from, OrderStatus to)
        => Assert.True(OrderService.IsValidTransition(from, to));

    [Theory]
    [InlineData(OrderStatus.Requested, OrderStatus.Completed)]   // saltarse todo
    [InlineData(OrderStatus.Requested, OrderStatus.InProgress)]
    [InlineData(OrderStatus.Assigned, OrderStatus.Completed)]
    [InlineData(OrderStatus.EnRoute, OrderStatus.Completed)]
    [InlineData(OrderStatus.InProgress, OrderStatus.Completed)]  // sin firma
    public void Rechaza_saltarse_pasos(OrderStatus from, OrderStatus to)
        => Assert.False(OrderService.IsValidTransition(from, to));

    [Theory]
    [InlineData(OrderStatus.Assigned, OrderStatus.Requested)]
    [InlineData(OrderStatus.InProgress, OrderStatus.Assigned)]
    [InlineData(OrderStatus.Completed, OrderStatus.InProgress)]
    public void Rechaza_retroceder(OrderStatus from, OrderStatus to)
        => Assert.False(OrderService.IsValidTransition(from, to));

    [Fact]
    public void Completed_es_un_estado_final()
    {
        foreach (var to in Enum.GetValues<OrderStatus>())
        {
            Assert.False(OrderService.IsValidTransition(OrderStatus.Completed, to));
        }
    }

    [Fact]
    public void Ningun_estado_transiciona_a_si_mismo()
    {
        foreach (var s in Enum.GetValues<OrderStatus>())
        {
            Assert.False(OrderService.IsValidTransition(s, s));
        }
    }
}
