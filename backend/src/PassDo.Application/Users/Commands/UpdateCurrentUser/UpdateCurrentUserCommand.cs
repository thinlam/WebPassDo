using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Users.DTOs;
using PassDo.Application.Users.Mappings;

namespace PassDo.Application.Users.Commands.UpdateCurrentUser;

public record UpdateCurrentUserCommand(
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl) : IRequest<UserDto>;

public class UpdateCurrentUserCommandValidator : AbstractValidator<UpdateCurrentUserCommand>
{
    public UpdateCurrentUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));
    }
}

public class UpdateCurrentUserCommandHandler : IRequestHandler<UpdateCurrentUserCommand, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCurrentUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto> Handle(UpdateCurrentUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == _currentUserService.UserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User", _currentUserService.UserId.Value);
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.AvatarUrl = request.AvatarUrl?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return UserMapper.ToDto(user);
    }
}
