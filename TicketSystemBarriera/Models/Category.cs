namespace TicketSystemBarriera.Models
{
    public class Category
    {
        public int Id { get; set; }
        public required string name { get; set; }
        public string? description { get; set; }

    }
}
