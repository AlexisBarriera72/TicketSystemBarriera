using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Server.Api;

// Mapeo entidad → DTO. Las entidades de EF nunca salen por la API.
public static class MappingExtensions
{
    public static OrderDto ToDto(this Order o) => new(
        o.Id, o.Title, o.Description, o.Status, o.Priority,
        o.CategoryId, o.Category?.Name, o.CreatedAt, o.UpdatedAt,
        o.AuthorId, o.Author?.DisplayName ?? o.Author?.Email,
        o.AssignedDriverId, o.AssignedDriver?.DisplayName ?? o.AssignedDriver?.Email);

    public static MessageDto ToDto(this Message m) => new(
        m.Id, m.OrderId, m.Content, m.CreatedAt, m.UserId,
        m.User?.DisplayName ?? m.User?.Email,
        m.IsSystem, m.SenderRole,
        m.AttachmentPath is not null,
        m.Latitude, m.Longitude, m.CapturedAtUtc, m.Stage);

    public static CategoryDto ToDto(this Category c) => new(c.Id, c.Name, c.Description);

    public static UserSummaryDto ToDto(this ApplicationUser u, IEnumerable<string>? roles = null) =>
        new(u.Id, u.DisplayName, u.Email, roles?.ToList() ?? []);

    public static SignatureDocumentDto ToDto(this SignatureDocument d) => new(
        d.Id, d.OrderId, d.Status, d.IsProvisional, d.SignerName,
        d.CreatedAtUtc, d.SignedAtUtc, d.SignedCapturedAtUtc,
        d.Latitude, d.Longitude, d.ContentHash, d.RejectReason,
        d.ReviewedBy?.DisplayName ?? d.ReviewedBy?.Email, d.ReviewedAtUtc,
        d.EmailStatus);

    public static ComplaintDto ToDto(this Complaint c) => new(
        c.Id, c.Subject, c.Description, c.Status, c.OrderId,
        c.ClientUserId, c.Client?.DisplayName ?? c.Client?.Email,
        c.CreatedAtUtc, c.OfficeResponse,
        c.RespondedBy?.DisplayName ?? c.RespondedBy?.Email, c.RespondedAtUtc);

    public static DirectMessageDto ToDto(this DirectMessage m) => new(
        m.Id, m.ConversationId, m.Content, m.CreatedAt, m.SenderUserId,
        m.Sender?.DisplayName ?? m.Sender?.Email, m.SenderRole, m.CapturedAtUtc);

    public static PaperworkDocumentDto ToDto(this PaperworkDocument p) => new(
        p.Id, p.OrderId, p.SlotKey, p.Status, p.IsPdf,
        p.CreatedAtUtc, p.CapturedAtUtc, p.Latitude, p.Longitude,
        p.ContentHash, p.RejectReason,
        p.ReviewedBy?.DisplayName ?? p.ReviewedBy?.Email, p.ReviewedAtUtc);

    public static TimeEntryDto ToDto(this TimeEntry t) => new(
        t.Id, t.UserId, t.User?.DisplayName ?? t.User?.Email,
        t.ClockInUtc, t.ClockOutUtc,
        t.ClockInLatitude, t.ClockInLongitude,
        t.ClockOutLatitude, t.ClockOutLongitude,
        t.AutoClosed,
        t.ClockInCapturedAtUtc, t.ClockOutCapturedAtUtc);
}
