using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services;

public interface IComplaintService
{
    Task<Complaint> CreateAsync(string clientUserId, string subject, string description, int? orderId);
    Task<List<Complaint>> GetForClientAsync(string clientUserId);
    Task<List<Complaint>> GetAllAsync();
    Task<Complaint?> GetByIdAsync(int id);
    Task<(bool Ok, string? Error)> RespondAsync(int id, string reviewerUserId, string response, bool resolve);
}
