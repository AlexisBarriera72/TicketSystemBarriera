using Microsoft.AspNetCore.Identity;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        // 1. Roles del negocio
        string[] roleNames = [Roles.Admin, Roles.Office, Roles.Driver, Roles.Client];
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Admin por defecto — credenciales vienen de user-secrets / variables de entorno,
        //    nunca del código ni de appsettings.json (regla: no secrets en git)
        var adminEmail = config["Seed:AdminEmail"];
        var adminPassword = config["Seed:AdminPassword"];
        if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DisplayName = "Administrador"
                };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                }
                else
                {
                    logger.LogWarning("No se pudo crear el usuario admin: {Errors}",
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }
        }
        else
        {
            logger.LogWarning(
                "Seed:AdminEmail / Seed:AdminPassword no están configurados; no se creó el admin. " +
                "Configúralos con 'dotnet user-secrets set'.");
        }

        // 3. Tipos de mudanza
        if (!context.Categories.Any())
        {
            context.Categories.AddRange(
                new Category { Name = "Mudanza Local", Description = "Dentro de la misma ciudad o área" },
                new Category { Name = "Larga Distancia", Description = "Entre ciudades o estados" },
                new Category { Name = "Solo Empaque", Description = "Servicio de empaque sin transporte" },
                new Category { Name = "Almacenamiento", Description = "Guardar pertenencias en almacén" }
            );
            await context.SaveChangesAsync();
        }
    }
}
