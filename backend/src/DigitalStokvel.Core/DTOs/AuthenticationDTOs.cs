namespace DigitalStokvel.Core.DTOs;

public record LoginRequest(
    string PhoneNumber,
    string Password);

public record LoginResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User);

public record RegisterRequest(
    string PhoneNumber,
    string Password,
    string PreferredLanguage);

public record RegisterResponse(
    string UserId,
    string Message);

public record UserInfo(
    string UserId,
    string PhoneNumber,
    string? PreferredLanguage,
    bool FicaVerified);

public record RefreshTokenRequest(
    string Token,
    string RefreshToken);
