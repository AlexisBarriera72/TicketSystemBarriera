using Microsoft.AspNetCore.Identity;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Services;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Server.Api;

// Categorías (tipos de mudanza), usuarios y reportes
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogApi(this IEndpointRouteBuilder app)
    {
        app.MapGroup(ApiRoutes.Categories)
            .WithTags("Categories")
            .RequireAuthorization(ApiAuth.Policy)
            .MapGet("/", async (IOrderService orders) =>
                Results.Ok((await orders.GetCategoriesAsync()).Select(c => c.ToDto())));

        var users = app.MapGroup(ApiRoutes.Users)
            .WithTags("Users")
            .RequireAuthorization(ApiAuth.StaffPolicy);

        // ?role=Driver filtra por rol; sin filtro devuelve todos con sus roles
        users.MapGet("/", async (string? role, IOrderService orders,
            UserManager<ApplicationUser> userManager) =>
        {
            var list = string.IsNullOrEmpty(role)
                ? await orders.GetAllUsersAsync()
                : await orders.GetUsersByRoleAsync(role);

            var result = new List<UserSummaryDto>();
            foreach (var u in list)
            {
                var roles = await userManager.GetRolesAsync(u);
                result.Add(u.ToDto(roles));
            }
            return Results.Ok(result);
        });

        var reports = app.MapGroup(ApiRoutes.Reports)
            .WithTags("Reports")
            .RequireAuthorization(ApiAuth.StaffPolicy);

        reports.MapGet("/stats", async (IReportService reportService) =>
            Results.Ok(await reportService.GetOrderStatsAsync()));

        reports.MapGet("/orders.xlsx", async (IReportService reportService) =>
            Results.File(await reportService.GenerateExcelReportAsync(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Reporte_Ordenes_{DateTime.Now:yyyyMMdd}.xlsx"));

        return app;
    }
}
