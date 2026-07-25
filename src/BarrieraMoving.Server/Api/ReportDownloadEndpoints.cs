using Microsoft.AspNetCore.Authorization;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Services;

namespace BarrieraMoving.Server.Api;

// Descarga del informe Excel desde el DASHBOARD WEB (cookie de Identity), no
// desde la API móvil. Se sirve por HTTP para que el navegador lo descargue de
// forma nativa, en vez de mandar el fichero en Base64 por el circuito SignalR.
public static class ReportDownloadEndpoints
{
    public static IEndpointRouteBuilder MapReportDownloads(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/reports/ordenes.xlsx", async (HttpContext http, IReportService reports) =>
        {
            var name = $"Reporte_Ordenes_{DateTime.Now:yyyyMMdd}.xlsx";
            http.Response.ContentType =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            http.Response.Headers.ContentDisposition = $"attachment; filename=\"{name}\"";
            await reports.WriteExcelReportAsync(http.Response.Body);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Roles.Admin},{Roles.Office}" })
        .WithTags("Reports");

        return app;
    }
}
