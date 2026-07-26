using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using PassDo.Application.Common.Interfaces;
using PassDo.Infrastructure.Options;

namespace PassDo.Infrastructure.Identity;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleAuthOptions _options;

    public GoogleTokenValidator(IOptions<GoogleAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<GoogleUserPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException("Google ClientId is not configured.");
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _options.ClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleUserPayload(
                payload.Subject,
                payload.Email,
                payload.EmailVerified,
                payload.Name,
                payload.Picture);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
