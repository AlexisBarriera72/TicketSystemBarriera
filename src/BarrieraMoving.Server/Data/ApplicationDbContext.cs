using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<TimeEntry> TimeEntries { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Order>()
            .HasOne(o => o.Author)
            .WithMany()
            .HasForeignKey(o => o.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasOne(o => o.AssignedDriver)
            .WithMany()
            .HasForeignKey(o => o.AssignedDriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TimeEntry>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Si se borra la orden, el registro de horas sobrevive sin orden asociada
        builder.Entity<TimeEntry>()
            .HasOne(t => t.Order)
            .WithMany()
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Garantía a nivel de BD: UNA sola jornada abierta por empleado.
        // Dos clock-in simultáneos no pueden colarse (es dato de nómina).
        builder.Entity<TimeEntry>()
            .HasIndex(t => t.UserId)
            .HasFilter("[ClockOutUtc] IS NULL")
            .IsUnique()
            .HasDatabaseName("IX_TimeEntries_OneOpenPerUser");

        builder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        // Idempotencia de la cola offline: el mismo envío reintentado no inserta dos veces
        builder.Entity<Message>()
            .HasIndex(m => m.IdempotencyKey)
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .IsUnique();

        builder.Entity<TimeEntry>()
            .HasIndex(t => t.ClockInIdempotencyKey)
            .HasFilter("[ClockInIdempotencyKey] IS NOT NULL")
            .IsUnique();

        builder.Entity<TimeEntry>()
            .HasIndex(t => t.ClockOutIdempotencyKey)
            .HasFilter("[ClockOutIdempotencyKey] IS NOT NULL")
            .IsUnique();
    }
}
