namespace BarrieraMoving.Shared.Dtos;

// Métricas operativas de cabecera (barra de quick-stats). Un solo payload:
// el servicio lo calcula con contadores agregados, no en varias vueltas.
// ActiveStaff y PendingComplaints son SENSIBLES: solo se renderizan para
// Admin/Oficina (nunca llegan al navegador de un cliente).
public record QuickStatsDto(
    int ActiveStaff,        // personal fichado ahora (staff only)
    int ActiveMoves,        // InProgress + EnRoute
    int CompletedOrders,    // Completed (público)
    int PendingComplaints,  // reclamaciones sin resolver (staff only)
    int ClientsServed);     // clientes distintos con una mudanza completada (público)
