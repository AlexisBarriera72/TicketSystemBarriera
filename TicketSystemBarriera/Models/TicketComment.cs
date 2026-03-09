namespace TicketSystemBarriera.Models
{
    public class TicketComment
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }
        public required string UserId { get; set; }
        public Data.ApplicationUser? User { get; set; }
    }
}
