using Microsoft.AspNetCore.Identity;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Shared;
using BarrieraMoving.Shared.Dtos;

namespace BarrieraMoving.Server.Api;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Auth).WithTags("Auth");

        // Login con email + password → JWT + refresh token
        group.MapPost("/login", async (LoginRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.Unauthorized();

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null) return Results.Unauthorized();

            // Respeta lockout y confirmación de cuenta igual que el login web
            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded) return Results.Unauthorized();

            return Results.Ok(await tokenService.IssueTokensAsync(user));
        });

        // Intercambia un refresh token válido por un nuevo par de tokens
        group.MapPost("/refresh", async (RefreshRequest request, TokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken)) return Results.Unauthorized();

            var user = await tokenService.RedeemRefreshTokenAsync(request.RefreshToken);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(await tokenService.IssueTokensAsync(user));
        });

        // Logout: revoca el refresh token en el servidor (el cliente borra los suyos).
        // Devuelve 204 siempre para no revelar si el token era válido.
        group.MapPost("/logout", async (RefreshRequest request, TokenService tokenService) =>
        {
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                await tokenService.RevokeRefreshTokenAsync(request.RefreshToken);
            }
            return Results.NoContent();
        });

        return app;
    }
}
