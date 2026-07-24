using PassDo.Application.Auth.DTOs;
using PassDo.Domain.Entities;

namespace PassDo.Application.Auth.Services;

public interface IAuthSessionService
{
    Task<AuthResponseDto> CreateSessionAsync(User user, string? clientIp, CancellationToken cancellationToken);
    Task<AuthResponseDto> RefreshSessionAsync(string refreshToken, string? clientIp, CancellationToken cancellationToken);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
}
