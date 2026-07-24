using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;

namespace PassDo.Application.Favorites.Commands.RemoveFavorite;

public record RemoveFavoriteCommand(Guid ProductId) : IRequest;

public class RemoveFavoriteCommandValidator : AbstractValidator<RemoveFavoriteCommand>
{
    public RemoveFavoriteCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFavoriteCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(
                x => x.UserId == _currentUserService.UserId && x.ProductId == request.ProductId,
                cancellationToken);

        if (favorite is null)
        {
            throw new NotFoundException("Favorite", request.ProductId);
        }

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
