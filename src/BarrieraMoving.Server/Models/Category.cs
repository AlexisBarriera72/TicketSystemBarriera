namespace BarrieraMoving.Server.Models;

// Tipo de mudanza (Local, Larga distancia, Solo empaque, etc.)
public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
