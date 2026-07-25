namespace BarrieraMoving.Server.Services;

public interface IReportService
{
    Task<Dictionary<string, int>> GetOrderStatsAsync();
    Task<Shared.Dtos.QuickStatsDto> GetQuickStatsAsync();
    Task<Dictionary<int, OrderDocSummary>> GetOrderDocSummariesAsync();
    Task<byte[]> GenerateExcelReportAsync();

    // Escribe el .xlsx directamente en el stream de salida (respuesta HTTP), sin
    // pasar por byte[] + Base64 + SignalR como hacía la descarga por JS interop.
    Task WriteExcelReportAsync(Stream destination);
}
