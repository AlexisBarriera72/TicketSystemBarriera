using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

// Resumen de documentos de una orden, para el dashboard y el Excel
public record OrderDocSummary(int PaperworkAttached, int PaperworkRequired,
    bool PaperworkComplete, SignatureDocStatus? SignatureStatus);

// Estadísticas y reportes del jefe (KPIs del dashboard + export a Excel)
public class ReportService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IPaperworkService paperwork) : IReportService
{
    // Barra de métricas operativas (quick-stats). Un DTO, contadores agregados:
    // los dos recuentos de órdenes se resuelven en UNA consulta; el resto son
    // COUNT indexados (uno por tabla — tablas distintas no comparten roundtrip).
    public async Task<Shared.Dtos.QuickStatsDto> GetQuickStatsAsync()
    {
        using var context = dbFactory.CreateDbContext();

        var orders = await context.Orders
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Completed = g.Count(o => o.Status == OrderStatus.Completed),
                Active = g.Count(o => o.Status == OrderStatus.InProgress || o.Status == OrderStatus.EnRoute),
            })
            .FirstOrDefaultAsync();

        var activeStaff = await context.TimeEntries.CountAsync(t => t.ClockOutUtc == null);
        var pending = await context.Complaints.CountAsync(c => c.Status != ComplaintStatus.Resolved);
        var clientsServed = await context.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .Select(o => o.AuthorId)
            .Distinct()
            .CountAsync();

        return new Shared.Dtos.QuickStatsDto(
            activeStaff, orders?.Active ?? 0, orders?.Completed ?? 0, pending, clientsServed);
    }

    // Estado de documentos por orden (papeleo obligatorio adjunto + firma)
    public async Task<Dictionary<int, OrderDocSummary>> GetOrderDocSummariesAsync()
    {
        using var context = dbFactory.CreateDbContext();
        var requiredKeys = paperwork.GetSlots().Where(s => s.Required).Select(s => s.Key).ToHashSet();
        var requiredCount = requiredKeys.Count;

        // Papeleo vigente (Attached) por orden y slot
        var attached = await context.PaperworkDocuments
            .Where(p => p.Status == PaperworkStatus.Attached)
            .Select(p => new { p.OrderId, p.SlotKey })
            .ToListAsync();

        var latestSig = await context.SignatureDocuments
            .GroupBy(s => s.OrderId)
            .Select(g => new { OrderId = g.Key, Status = g.OrderByDescending(s => s.Id).First().Status })
            .ToDictionaryAsync(x => x.OrderId, x => x.Status);

        var result = new Dictionary<int, OrderDocSummary>();
        foreach (var orderId in attached.Select(a => a.OrderId).Concat(latestSig.Keys).Distinct())
        {
            var have = attached.Where(a => a.OrderId == orderId && requiredKeys.Contains(a.SlotKey))
                .Select(a => a.SlotKey).Distinct().Count();
            result[orderId] = new OrderDocSummary(have, requiredCount, have >= requiredCount,
                latestSig.TryGetValue(orderId, out var st) ? st : null);
        }
        return result;
    }

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
    // Vuelca el informe en el stream destino. ClosedXML necesita un buffer con
    // posicionamiento para cerrar el ZIP, así que se arma en memoria UNA vez y se
    // copia; aun así evitamos el byte[] extra y el Base64 (+33%) del interop JS.
    public async Task WriteExcelReportAsync(Stream destination)
    {
        using var buffer = new MemoryStream();
        await BuildExcelAsync(buffer);
        buffer.Position = 0;
        await buffer.CopyToAsync(destination);
    }

    // Compatibilidad: sigue existiendo para quien necesite los bytes en memoria.
    public async Task<byte[]> GenerateExcelReportAsync()
    {
        using var buffer = new MemoryStream();
        await BuildExcelAsync(buffer);
        return buffer.ToArray();
    }

    private async Task BuildExcelAsync(Stream destination)
    {
        using var context = dbFactory.CreateDbContext();
        var orders = await context.Orders
            .Include(o => o.Author)
            .Include(o => o.AssignedDriver)
            .Include(o => o.Category)
            .ToListAsync();

        var docSummaries = await GetOrderDocSummariesAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Resumen de Órdenes");

        var headers = new[] { "ID", "Título", "Tipo", "Prioridad", "Estado", "Creado", "Completado",
            "Tiempo (Días)", "Cliente", "Conductor", "Papeleo", "Firma" };
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
            var summary = docSummaries.GetValueOrDefault(o.Id);
            worksheet.Cell(row, 11).Value = summary is null ? "0/0"
                : $"{summary.PaperworkAttached}/{summary.PaperworkRequired}";
            worksheet.Cell(row, 12).Value = summary?.SignatureStatus?.ToString() ?? "—";
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
            "Cierre automático", "Ubicación entrada", "Ubicación salida",
            "Pulsada entrada (dispositivo, UTC)", "Pulsada salida (dispositivo, UTC)" };
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
            // Hora del dispositivo (metadato no fiable) — solo si difiere de la del servidor
            if (t.ClockInCapturedAtUtc.HasValue) timeSheet.Cell(timeRow, 9).Value = t.ClockInCapturedAtUtc.Value;
            if (t.ClockOutCapturedAtUtc.HasValue) timeSheet.Cell(timeRow, 10).Value = t.ClockOutCapturedAtUtc.Value;
            timeRow++;
        }
        timeSheet.Columns().AdjustToContents();

        // Hoja 3: empleados (rol + horas totales trabajadas)
        var users = await context.Users.OrderBy(u => u.Email).ToListAsync();
        var roleById = await (from ur in context.UserRoles
                              join r in context.Roles on ur.RoleId equals r.Id
                              select new { ur.UserId, r.Name })
                             .ToDictionaryAsync(x => x.UserId, x => x.Name);
        var hoursByUser = timeEntries
            .Where(t => t.ClockOutUtc.HasValue)
            .GroupBy(t => t.UserId)
            .ToDictionary(g => g.Key, g => g.Sum(t => (t.ClockOutUtc!.Value - t.ClockInUtc).TotalHours));

        var empSheet = workbook.Worksheets.Add("Empleados");
        var empHeaders = new[] { "Nombre", "Email", "Rol", "Horas registradas", "Jornada abierta" };
        for (int i = 0; i < empHeaders.Length; i++)
        {
            empSheet.Cell(1, i + 1).Value = empHeaders[i];
            empSheet.Cell(1, i + 1).Style.Font.Bold = true;
            empSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        int empRow = 2;
        foreach (var u in users)
        {
            empSheet.Cell(empRow, 1).Value = u.DisplayName ?? "";
            empSheet.Cell(empRow, 2).Value = u.Email;
            empSheet.Cell(empRow, 3).Value = roleById.GetValueOrDefault(u.Id) ?? "—";
            empSheet.Cell(empRow, 4).Value = Math.Round(hoursByUser.GetValueOrDefault(u.Id), 2);
            empSheet.Cell(empRow, 5).Value = timeEntries.Any(t => t.UserId == u.Id && t.ClockOutUtc is null) ? "SÍ" : "";
            empRow++;
        }
        empSheet.Columns().AdjustToContents();

        // Hoja 4: documentos por orden (papeleo detallado + estado de firma)
        var slots = paperwork.GetSlots().ToList();
        var allPaperwork = await context.PaperworkDocuments
            .Where(p => p.Status != PaperworkStatus.Replaced)
            .ToListAsync();

        var docSheet = workbook.Worksheets.Add("Documentos");
        var docHeaders = new List<string> { "Orden" };
        docHeaders.AddRange(slots.Select(s => s.Label));
        docHeaders.Add("Firma");
        for (int i = 0; i < docHeaders.Count; i++)
        {
            docSheet.Cell(1, i + 1).Value = docHeaders[i];
            docSheet.Cell(1, i + 1).Style.Font.Bold = true;
            docSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        int docRow = 2;
        foreach (var o in orders)
        {
            docSheet.Cell(docRow, 1).Value = $"#{o.Id} {o.Title}";
            for (int i = 0; i < slots.Count; i++)
            {
                var doc = allPaperwork
                    .Where(p => p.OrderId == o.Id && p.SlotKey == slots[i].Key)
                    .OrderByDescending(p => p.CreatedAtUtc).FirstOrDefault();
                docSheet.Cell(docRow, i + 2).Value = doc?.Status.ToString() ?? "Pendiente";
            }
            docSheet.Cell(docRow, slots.Count + 2).Value = docSummaries.GetValueOrDefault(o.Id)?.SignatureStatus?.ToString() ?? "—";
            docRow++;
        }
        docSheet.Columns().AdjustToContents();

        workbook.SaveAs(destination);
    }
}
