namespace BarrieraMoving.Shared.Dtos;

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record TokenResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    UserSummaryDto User);
