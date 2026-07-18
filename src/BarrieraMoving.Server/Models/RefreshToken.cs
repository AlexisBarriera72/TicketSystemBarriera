namespace BarrieraMoving.Server.Models;

using BarrieraMoving.Server.Data;

// Refresh token para los clientes que usan la API (MAUI). Se guarda el hash, nunca el token.
public class RefreshToken
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedUtc { get; set; }

    public bool IsActive => RevokedUtc is null && DateTime.UtcNow < ExpiresUtc;
}
