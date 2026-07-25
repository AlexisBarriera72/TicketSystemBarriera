using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Tests;

// Base de datos SQLite EN MEMORIA por test. Se usa un proveedor relacional real
// (no InMemory) para que los índices únicos y las restricciones se comporten
// como en producción: los tests de idempotencia dependen de eso.
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public IDbContextFactory<ApplicationDbContext> Factory { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); // mantener abierta = la BD vive mientras dure el test

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Factory = new PooledFactory(options);
        using var ctx = Factory.CreateDbContext();
        ctx.Database.EnsureCreated();

        // Usuarios de prueba. SQLite SÍ aplica las claves ajenas (como SQL Server),
        // así que las órdenes y los fichajes necesitan usuarios reales detrás.
        foreach (var id in new[]
                 { "cliente-1", "otro-cliente", "conductor-1", "conductor-A",
                   "conductor-B", "conductor-distinto", "otro-conductor", "oficina-1" })
        {
            ctx.Users.Add(new ApplicationUser
            {
                Id = id,
                UserName = $"{id}@test.local",
                NormalizedUserName = $"{id}@TEST.LOCAL",
                Email = $"{id}@test.local",
                NormalizedEmail = $"{id}@TEST.LOCAL",
                DisplayName = id,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
            });
        }
        // Toda orden apunta a una categoría (tipo de mudanza)
        ctx.Categories.Add(new Category { Id = SeedCategoryId, Name = "Mudanza Local" });
        ctx.SaveChanges();
    }

    // Categoría lista para usar en las órdenes de prueba
    public const int SeedCategoryId = 1;

    public ApplicationDbContext NewContext() => Factory.CreateDbContext();

    public void Dispose() => _connection.Dispose();

    private sealed class PooledFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }
}
