using Microsoft.EntityFrameworkCore;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

public class ComplaintService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    INotificationService notify) : IComplaintService
{
    public async Task<Complaint> CreateAsync(string clientUserId, string subject, string description, int? orderId)
    {
        using var context = dbFactory.CreateDbContext();
        var complaint = new Complaint
        {
            ClientUserId = clientUserId,
            Subject = subject.Trim(),
            Description = description.Trim(),
            OrderId = orderId,
        };
        context.Complaints.Add(complaint);
        await context.SaveChangesAsync();
        return complaint;
    }

    public async Task<List<Complaint>> GetForClientAsync(string clientUserId)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.Complaints
            .Include(c => c.RespondedBy)
            .Where(c => c.ClientUserId == clientUserId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<List<Complaint>> GetAllAsync()
    {
        using var context = dbFactory.CreateDbContext();
        return await context.Complaints
            .Include(c => c.Client)
            .Include(c => c.RespondedBy)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<Complaint?> GetByIdAsync(int id)
    {
        using var context = dbFactory.CreateDbContext();
        return await context.Complaints
            .Include(c => c.Client)
            .Include(c => c.RespondedBy)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<(bool, string?)> RespondAsync(int id, string reviewerUserId, string response, bool resolve)
    {
        if (string.IsNullOrWhiteSpace(response))
            return (false, "La respuesta no puede estar vacía.");

        using var context = dbFactory.CreateDbContext();
        var complaint = await context.Complaints.FindAsync(id);
        if (complaint is null) return (false, "La reclamación no existe.");

        complaint.OfficeResponse = response.Trim();
        complaint.RespondedByUserId = reviewerUserId;
        complaint.RespondedAtUtc = DateTime.UtcNow;
        complaint.Status = resolve ? ComplaintStatus.Resolved : ComplaintStatus.InReview;
        await context.SaveChangesAsync();

        // Push al cliente. En capa de servicio → dispara también desde el dashboard
        // web (donde la oficina responde habitualmente).
        await notify.NotifyComplaintResponseAsync(id);
        return (true, null);
    }
}
