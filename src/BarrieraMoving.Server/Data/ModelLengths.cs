using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Data;

// Longitudes máximas de TODAS las columnas de texto, en un solo sitio.
// Sin esto EF las crea como nvarchar(max): una fila puede crecer sin límite y un
// cliente (o un script contra el formulario público) puede llenar el disco.
// También permite a SQL Server indexar y estimar cardinalidad mucho mejor.
public static class ModelLengths
{
    // Claves de ASP.NET Identity (nvarchar(450) por convención del framework)
    private const int Id = 450;
    private const int Idem = 64;    // GUID "N"/"D" de la cola offline
    private const int Hash = 128;   // SHA-256 en hex = 64; holgura para el futuro
    private const int Role = 32;
    private const int Name = 120;
    private const int Title = 200;
    private const int Email = 256;
    private const int Phone = 40;
    private const int Short = 100;  // zonas, tipo de servicio, fecha aproximada
    private const int Path = 400;
    private const int Text = 2000;  // mensajes y descripciones
    private const int Reason = 500;

    public static void Apply(ModelBuilder b)
    {
        // Nombre real del usuario (obligatorio al registrarse)
        b.Entity<ApplicationUser>(e => e.Property(x => x.DisplayName).HasMaxLength(Name));

        b.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(Title);
            e.Property(x => x.Description).HasMaxLength(Text);
        });

        b.Entity<Order>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(Title);
            e.Property(x => x.Description).HasMaxLength(Text);
            e.Property(x => x.AuthorId).HasMaxLength(Id);
            e.Property(x => x.AssignedDriverId).HasMaxLength(Id);
            e.Property(x => x.TrackingToken).HasMaxLength(Idem); // 32 bytes en Base64url = 43
            e.Property(x => x.Floor).HasMaxLength(Short);
            e.Property(x => x.Items).HasMaxLength(Text);
        });

        b.Entity<Message>(e =>
        {
            e.Property(x => x.Content).HasMaxLength(Text);
            e.Property(x => x.UserId).HasMaxLength(Id);
            e.Property(x => x.SenderRole).HasMaxLength(Role);
            e.Property(x => x.AttachmentPath).HasMaxLength(Path);
            e.Property(x => x.AttachmentThumbPath).HasMaxLength(Path);
            e.Property(x => x.IdempotencyKey).HasMaxLength(Idem);
        });

        b.Entity<TimeEntry>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(Id);
            e.Property(x => x.ClockInIdempotencyKey).HasMaxLength(Idem);
            e.Property(x => x.ClockOutIdempotencyKey).HasMaxLength(Idem);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(Id);
            e.Property(x => x.TokenHash).HasMaxLength(Hash);
        });

        b.Entity<SignatureDocument>(e =>
        {
            e.Property(x => x.RequestedByUserId).HasMaxLength(Id);
            e.Property(x => x.ProviderEnvelopeId).HasMaxLength(Title);
            e.Property(x => x.SignerName).HasMaxLength(Name);
            e.Property(x => x.SignerEmail).HasMaxLength(Email);
            e.Property(x => x.PdfPath).HasMaxLength(Path);
            e.Property(x => x.ContentHash).HasMaxLength(Hash);
            e.Property(x => x.ReviewedByUserId).HasMaxLength(Id);
            e.Property(x => x.RejectReason).HasMaxLength(Reason);
            e.Property(x => x.EmailError).HasMaxLength(Text);
            e.Property(x => x.IdempotencyKey).HasMaxLength(Idem);
        });

        b.Entity<PaperworkDocument>(e =>
        {
            e.Property(x => x.SlotKey).HasMaxLength(Short);
            e.Property(x => x.UploadedByUserId).HasMaxLength(Id);
            e.Property(x => x.FilePath).HasMaxLength(Path);
            e.Property(x => x.ThumbPath).HasMaxLength(Path);
            e.Property(x => x.ContentHash).HasMaxLength(Hash);
            e.Property(x => x.RejectReason).HasMaxLength(Reason);
            e.Property(x => x.ReviewedByUserId).HasMaxLength(Id);
            e.Property(x => x.IdempotencyKey).HasMaxLength(Idem);
        });

        b.Entity<DirectConversation>(e =>
            e.Property(x => x.CreatedByUserId).HasMaxLength(Id));

        b.Entity<DirectParticipant>(e =>
            e.Property(x => x.UserId).HasMaxLength(Id));

        b.Entity<DirectMessage>(e =>
        {
            e.Property(x => x.SenderUserId).HasMaxLength(Id);
            e.Property(x => x.Content).HasMaxLength(Text);
            e.Property(x => x.SenderRole).HasMaxLength(Role);
            e.Property(x => x.IdempotencyKey).HasMaxLength(Idem);
        });

        b.Entity<Complaint>(e =>
        {
            e.Property(x => x.ClientUserId).HasMaxLength(Id);
            e.Property(x => x.Subject).HasMaxLength(Title);
            e.Property(x => x.Description).HasMaxLength(Text);
            e.Property(x => x.OfficeResponse).HasMaxLength(Text);
            e.Property(x => x.RespondedByUserId).HasMaxLength(Id);
        });

        b.Entity<DeviceToken>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(Id);
            e.Property(x => x.Token).HasMaxLength(Reason); // los tokens FCM rondan 160-200
            e.Property(x => x.Platform).HasMaxLength(Role);
        });

        // Formulario PÚBLICO: es el que más conviene acotar (anónimo)
        b.Entity<QuoteRequest>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(Name);
            e.Property(x => x.Phone).HasMaxLength(Phone);
            e.Property(x => x.Email).HasMaxLength(Email);
            e.Property(x => x.ServiceType).HasMaxLength(Short);
            e.Property(x => x.OriginZone).HasMaxLength(Short);
            e.Property(x => x.DestinationZone).HasMaxLength(Short);
            e.Property(x => x.PreferredDate).HasMaxLength(Short);
            e.Property(x => x.Details).HasMaxLength(Text);
            e.Property(x => x.ReferenceCode).HasMaxLength(Role);
            e.Property(x => x.Floor).HasMaxLength(Short);
            // Lista de artículos separados por "|": el catálogo entero cabe de sobra
            e.Property(x => x.Items).HasMaxLength(Text);
            e.Property(x => x.ClaimedByUserId).HasMaxLength(Id);
        });

        b.Entity<QuotePhoto>(e =>
        {
            e.Property(x => x.FilePath).HasMaxLength(Path);
            e.Property(x => x.ThumbPath).HasMaxLength(Path);
            // Si se borra la solicitud, sus fotos se van con ella
            e.HasOne(x => x.QuoteRequest)
             .WithMany(q => q.Photos)
             .HasForeignKey(x => x.QuoteRequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
