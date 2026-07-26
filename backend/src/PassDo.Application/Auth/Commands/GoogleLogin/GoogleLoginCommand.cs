using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Auth.DTOs;
using PassDo.Application.Auth.Services;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;

namespace PassDo.Application.Auth.Commands.GoogleLogin;

public record GoogleLoginCommand(string IdToken, string? ClientIp) : IRequest<AuthResponseDto>;

public class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
    }
}

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly IAuthSessionService _authSessionService;

    public GoogleLoginCommandHandler(
        IApplicationDbContext context,
        IGoogleTokenValidator googleTokenValidator,
        IAuthSessionService authSessionService)
    {
        _context = context;
        _googleTokenValidator = googleTokenValidator;
        _authSessionService = authSessionService;
    }

    public async Task<AuthResponseDto> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var payload = await _googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken)
            ?? throw new UnauthorizedException("Invalid Google token.");

        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new UnauthorizedException("Google account email is not verified.");
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.GoogleSubject == payload.Subject, cancellationToken);

        if (user is null)
        {
            user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

            if (user is not null)
            {
                // Link existing (email/password) account to this Google identity.
                user.GoogleSubject = payload.Subject;
                if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(payload.Picture))
                {
                    user.AvatarUrl = payload.Picture;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        if (user is null)
        {
            user = new User
            {
                Email = normalizedEmail,
                FullName = string.IsNullOrWhiteSpace(payload.Name) ? normalizedEmail : payload.Name!.Trim(),
                PasswordHash = string.Empty,
                GoogleSubject = payload.Subject,
                AvatarUrl = payload.Picture,
                Role = UserRole.User,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User account is disabled.");
        }

        return await _authSessionService.CreateSessionAsync(user, request.ClientIp, cancellationToken);
    }
}
