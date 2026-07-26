namespace PassDo.Api.Contracts.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber);

public record LoginRequest(
    string Email,
    string Password);

/// <summary>Login via Google Sign-In. <c>IdToken</c> is the Google ID token issued to the frontend.</summary>
public record GoogleLoginRequest(string IdToken);

public record RefreshTokenRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
