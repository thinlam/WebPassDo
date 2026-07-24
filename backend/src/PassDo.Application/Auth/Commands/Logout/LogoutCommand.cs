using FluentValidation;
using MediatR;
using PassDo.Application.Auth.Services;

namespace PassDo.Application.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IAuthSessionService _authSessionService;

    public LogoutCommandHandler(IAuthSessionService authSessionService)
    {
        _authSessionService = authSessionService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _authSessionService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
    }
}
