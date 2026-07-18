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

        builder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();
    }
}
