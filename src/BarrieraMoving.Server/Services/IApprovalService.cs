using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Server.Services;

// Cola de aprobaciones para Oficina/Admin: órdenes con firma esperando aprobación
// o con papeleo rechazado/faltante. Es TIME-SENSITIVE (bloquea completar la orden),
// por eso alimenta un badge en el dashboard web.
public interface IApprovalService
{
    Task<List<ApprovalItemDto>> GetPendingApprovalsAsync();
    Task<int> GetPendingCountAsync();
}
