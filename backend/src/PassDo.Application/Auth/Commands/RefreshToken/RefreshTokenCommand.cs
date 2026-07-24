using FluentValidation;
using MediatR;
using PassDo.Application.Auth.DTOs;
using PassDo.Application.Auth.Services;

namespace PassDo.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken,
    string? ClientIp) : IRequest<AuthResponseDto>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IAuthSessionService _authSessionService;

    public RefreshTokenCommandHandler(IAuthSessionService authSessionService)
    {
        _authSessionService = authSessionService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return await _authSessionService.RefreshSessionAsync(
            request.RefreshToken,
            request.ClientIp,
            cancellationToken);
    }
}
