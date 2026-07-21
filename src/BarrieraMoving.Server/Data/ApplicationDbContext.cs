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
    public DbSet<SignatureDocument> SignatureDocuments { get; set; }
    public DbSet<PaperworkDocument> PaperworkDocuments { get; set; }
    public DbSet<DirectConversation> DirectConversations { get; set; }
    public DbSet<DirectParticipant> DirectParticipants { get; set; }
    public DbSet<DirectMessage> DirectMessages { get; set; }

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

        // Documentos de firma: registros legales — restrict en usuarios, idempotencia
        builder.Entity<SignatureDocument>()
            .HasOne(d => d.RequestedBy)
            .WithMany()
            .HasForeignKey(d => d.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SignatureDocument>()
            .HasOne(d => d.ReviewedBy)
            .WithMany()
            .HasForeignKey(d => d.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SignatureDocument>()
            .HasIndex(d => d.IdempotencyKey)
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .IsUnique();

        builder.Entity<SignatureDocument>()
            .HasIndex(d => d.ProviderEnvelopeId)
            .HasFilter("[ProviderEnvelopeId] IS NOT NULL")
            .IsUnique();

        // Papeleo obligatorio: mismo tratamiento de registro legal
        builder.Entity<PaperworkDocument>()
            .HasOne(p => p.UploadedBy)
            .WithMany()
            .HasForeignKey(p => p.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PaperworkDocument>()
            .HasOne(p => p.ReviewedBy)
            .WithMany()
            .HasForeignKey(p => p.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PaperworkDocument>()
            .HasIndex(p => p.IdempotencyKey)
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .IsUnique();

        builder.Entity<PaperworkDocument>()
            .HasIndex(p => new { p.OrderId, p.SlotKey });

        // Mensajería directa: ACL por pertenencia al conjunto de participantes
        builder.Entity<DirectConversation>()
            .HasOne(c => c.CreatedBy)
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DirectParticipant>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DirectParticipant>()
            .HasIndex(p => new { p.ConversationId, p.UserId })
            .IsUnique();

        builder.Entity<DirectMessage>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<DirectMessage>()
            .HasIndex(m => m.ConversationId);

        builder.Entity<DirectMessage>()
            .HasIndex(m => m.IdempotencyKey)
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .IsUnique();
    }
}
