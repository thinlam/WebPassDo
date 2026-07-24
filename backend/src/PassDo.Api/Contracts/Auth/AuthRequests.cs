namespace PassDo.Api.Contracts.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber);

public record LoginRequest(
    string Email,
    string Password);

public record RefreshTokenRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
