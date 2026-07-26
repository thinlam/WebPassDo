namespace PassDo.Application.Common.Interfaces;

public record GoogleUserPayload(
    string Subject,
    string Email,
    bool EmailVerified,
    string? Name,
    string? Picture);

public interface IGoogleTokenValidator
{
    /// <summary>Validates a Google ID token and returns the decoded payload, or null if invalid.</summary>
    Task<GoogleUserPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
