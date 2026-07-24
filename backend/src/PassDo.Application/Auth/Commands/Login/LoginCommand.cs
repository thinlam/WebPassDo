using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Auth.DTOs;
using PassDo.Application.Auth.Services;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;

namespace PassDo.Application.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password,
    string? ClientIp) : IRequest<AuthResponseDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthSessionService _authSessionService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IAuthSessionService authSessionService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _authSessionService = authSessionService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User account is disabled.");
        }

        return await _authSessionService.CreateSessionAsync(user, request.ClientIp, cancellationToken);
    }
}
