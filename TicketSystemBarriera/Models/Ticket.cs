namespace TicketSystemBarriera.Models;

using TicketSystemBarriera.Data;
using TicketSystemBarriera.Enums;
    public class Ticket
    {
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public required string AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }
    public string? TechnicianId { get; set; }
    public ApplicationUser? Technician {  get; set; }
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    }