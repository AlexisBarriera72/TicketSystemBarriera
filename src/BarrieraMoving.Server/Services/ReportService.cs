using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

// Estadísticas y reportes del jefe (KPIs del dashboard + export a Excel)
public class ReportService(IDbContextFactory<ApplicationDbContext> dbFactory) : IReportService
{
    public async Task<Dictionary<string, int>> GetOrderStatsAsync()
    {
        using var context = dbFactory.CreateDbContext();
        return new Dictionary<string, int>
        {
            ["Total"] = await context.Orders.CountAsync(),
            ["Active"] = await context.Orders.CountAsync(o =>
                o.Status != OrderStatus.Completed && o.Status != OrderStatus.PendingSignature),
            ["PendingSignature"] = await context.Orders.CountAsync(o => o.Status == OrderStatus.PendingSignature),
            ["Completed"] = await context.Orders.CountAsync(o => o.Status == OrderStatus.Completed),
            ["Urgent"] = await context.Orders.CountAsync(o => o.Priority == OrderPriority.Urgent)
        };
    }

    // Reporte Excel con el resumen de órdenes, usando ClosedXML
    public async Task<byte[]> GenerateExcelReportAsync()
    {
        using var context = dbFactory.CreateDbContext();
        var orders = await context.Orders
            .Include(o => o.Author)
            .Include(o => o.AssignedDriver)
            .Include(o => o.Category)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Resumen de Órdenes");

        var headers = new[] { "ID", "Título", "Tipo", "Prioridad", "Estado", "Creado", "Completado",
            "Tiempo (Días)", "Cliente", "Conductor" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var o in orders)
        {
            worksheet.Cell(row, 1).Value = o.Id;
            worksheet.Cell(row, 2).Value = o.Title;
            worksheet.Cell(row, 3).Value = o.Category?.Name ?? "N/A";
            worksheet.Cell(row, 4).Value = o.Priority.ToString();
            worksheet.Cell(row, 5).Value = o.Status.ToString();
            worksheet.Cell(row, 6).Value = o.CreatedAt;
            if (o.Status == OrderStatus.Completed && o.UpdatedAt.HasValue)
            {
                worksheet.Cell(row, 7).Value = o.UpdatedAt.Value;
                var duration = o.UpdatedAt.Value - o.CreatedAt;
                worksheet.Cell(row, 8).Value = Math.Round(duration.TotalDays, 2);
            }
            worksheet.Cell(row, 9).Value = o.Author?.Email;
            worksheet.Cell(row, 10).Value = o.AssignedDriver?.Email ?? "No asignado";
            row++;
        }
        worksheet.Columns().AdjustToContents();

        // Hoja 2: fichajes (clock-in/out) — datos de nómina
        var timeEntries = await context.TimeEntries
            .Include(t => t.User)
            .OrderByDescending(t => t.ClockInUtc)
            .ToListAsync();

        var timeSheet = workbook.Worksheets.Add("Fichajes");
        var timeHeaders = new[] { "ID", "Empleado", "Entrada (UTC)", "Salida (UTC)", "Horas",
            "Cierre automático", "Ubicación entrada", "Ubicación salida" };
        for (int i = 0; i < timeHeaders.Length; i++)
        {
            timeSheet.Cell(1, i + 1).Value = timeHeaders[i];
            timeSheet.Cell(1, i + 1).Style.Font.Bold = true;
            timeSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int timeRow = 2;
        foreach (var t in timeEntries)
        {
            timeSheet.Cell(timeRow, 1).Value = t.Id;
            timeSheet.Cell(timeRow, 2).Value = t.User?.DisplayName ?? t.User?.Email;
            timeSheet.Cell(timeRow, 3).Value = t.ClockInUtc;
            if (t.ClockOutUtc.HasValue)
            {
                timeSheet.Cell(timeRow, 4).Value = t.ClockOutUtc.Value;
                timeSheet.Cell(timeRow, 5).Value = Math.Round((t.ClockOutUtc.Value - t.ClockInUtc).TotalHours, 2);
            }
            timeSheet.Cell(timeRow, 6).Value = t.AutoClosed ? "SÍ — verificar horas" : "";
            timeSheet.Cell(timeRow, 7).Value = t.ClockInLatitude.HasValue
                ? $"{t.ClockInLatitude}, {t.ClockInLongitude}" : "";
            timeSheet.Cell(timeRow, 8).Value = t.ClockOutLatitude.HasValue
                ? $"{t.ClockOutLatitude}, {t.ClockOutLongitude}" : "";
            timeRow++;
        }
        timeSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
