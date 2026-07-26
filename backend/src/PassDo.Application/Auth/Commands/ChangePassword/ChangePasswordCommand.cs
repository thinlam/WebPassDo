using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Validation;

namespace PassDo.Application.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).MustBeStrongPassword();
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User", _currentUser.UserId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ConflictException("Mật khẩu hiện tại không đúng.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
