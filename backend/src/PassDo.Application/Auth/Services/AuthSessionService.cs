using Microsoft.EntityFrameworkCore;
using PassDo.Application.Auth.DTOs;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Users.Mappings;
using PassDo.Domain.Entities;

namespace PassDo.Application.Auth.Services;

public class AuthSessionService : IAuthSessionService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthSessionService(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AuthResponseDto> CreateSessionAsync(
        User user,
        string? clientIp,
        CancellationToken cancellationToken)
    {
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = _jwtTokenService.GetRefreshTokenExpiry();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = expiresAt,
            CreatedAt = _dateTimeProvider.UtcNow,
            CreatedByIp = clientIp
        });

        await _context.SaveChangesAsync(cancellationToken);

        return BuildResponse(user, refreshTokenValue, expiresAt);
    }

    public async Task<AuthResponseDto> RefreshSessionAsync(
        string refreshToken,
        string? clientIp,
        CancellationToken cancellationToken)
    {
        var storedToken = await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshToken, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        if (!storedToken.User.IsActive)
        {
            throw new UnauthorizedException("User account is disabled.");
        }

        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = _jwtTokenService.GetRefreshTokenExpiry();
        var utcNow = _dateTimeProvider.UtcNow;

        storedToken.RevokedAt = utcNow;
        storedToken.ReplacedByToken = newRefreshTokenValue;

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = storedToken.UserId,
            Token = newRefreshTokenValue,
            ExpiresAt = expiresAt,
            CreatedAt = utcNow,
            CreatedByIp = clientIp
        });

        await _context.SaveChangesAsync(cancellationToken);

        return BuildResponse(storedToken.User, newRefreshTokenValue, expiresAt);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken, cancellationToken);

        if (storedToken is null || storedToken.IsRevoked)
        {
            return;
        }

        storedToken.RevokedAt = _dateTimeProvider.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private AuthResponseDto BuildResponse(User user, string refreshToken, DateTime refreshExpiresAt)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = refreshExpiresAt,
            User = UserMapper.ToDto(user)
        };
    }
}
