namespace BarrieraMoving.Server.Services;

public interface IReportService
{
    Task<Dictionary<string, int>> GetOrderStatsAsync();
    Task<Shared.Dtos.QuickStatsDto> GetQuickStatsAsync();
    Task<Dictionary<int, OrderDocSummary>> GetOrderDocSummariesAsync();
    Task<byte[]> GenerateExcelReportAsync();
}
