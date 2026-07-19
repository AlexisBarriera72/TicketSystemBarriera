using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Server.Api;

// Emite access tokens (JWT) y refresh tokens para los clientes de la API (MAUI).
// El refresh token se guarda hasheado y se rota en cada uso.
public class TokenService(
    IConfiguration config,
    IDbContextFactory<ApplicationDbContext> dbFactory,
    UserManager<ApplicationUser> userManager)
{
    public async Task<TokenResponse> IssueTokensAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = CreateAccessToken(user, roles, out var expiresAt);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);
        return new TokenResponse(accessToken, expiresAt, refreshToken,
            new UserSummaryDto(user.Id, user.DisplayName, user.Email, [.. roles]));
    }

    // Valida y consume un refresh token; devuelve el usuario si es válido
    public async Task<ApplicationUser?> RedeemRefreshTokenAsync(string rawToken)
    {
        using var context = dbFactory.CreateDbContext();
        var hash = Hash(rawToken);
        var stored = await context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null || !stored.IsActive) return null;

        stored.RevokedUtc = DateTime.UtcNow; // rotación: cada refresh token se usa una sola vez
        await context.SaveChangesAsync();
        return stored.User;
    }

    private string CreateAccessToken(ApplicationUser user, IList<string> roles, out DateTime expiresAt)
    {
        var key = config["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey no está configurado (usa dotnet user-secrets).");
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var minutes = int.TryParse(config["Jwt:AccessTokenMinutes"], out var m) ? m : 60;
        expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.UserName ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(string userId)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var days = int.TryParse(config["Jwt:RefreshTokenDays"], out var d) ? d : 30;

        using var context = dbFactory.CreateDbContext();
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(raw),
            ExpiresUtc = DateTime.UtcNow.AddDays(days),
        });
        await context.SaveChangesAsync();
        return raw;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
