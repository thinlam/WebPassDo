using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Auth.DTOs;
using PassDo.Application.Auth.Services;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Domain.Entities;

namespace PassDo.Application.Auth.Commands.Login;

/// <summary>
/// Login accepts either an email address or a phone number in <paramref name="Email"/>
/// (property name kept for API/request compatibility - it represents "Email hoặc số điện thoại").
/// </summary>
public record LoginCommand(
    string Email,
    string Password,
    string? ClientIp) : IRequest<AuthResponseDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty();

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
        var identifier = request.Email.Trim();

        User? user;
        if (identifier.Contains('@'))
        {
            var normalizedEmail = identifier.ToLowerInvariant();
            user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        }
        else
        {
            user = await _context.Users
                .FirstOrDefaultAsync(x => x.PhoneNumber == identifier, cancellationToken);
        }

        if (user is null)
        {
            throw new UnauthorizedException("Invalid email/phone number or password.");
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new UnauthorizedException("This account uses Google sign-in. Please log in with Google.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email/phone number or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User account is disabled.");
        }

        return await _authSessionService.CreateSessionAsync(user, request.ClientIp, cancellationToken);
    }
}
