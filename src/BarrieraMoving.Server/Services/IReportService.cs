namespace BarrieraMoving.Server.Services;

public interface IReportService
{
    Task<Dictionary<string, int>> GetOrderStatsAsync();
    Task<byte[]> GenerateExcelReportAsync();
}
