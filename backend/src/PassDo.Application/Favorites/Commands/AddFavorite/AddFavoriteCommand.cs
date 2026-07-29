using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Favorites.DTOs;
using PassDo.Application.Products;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;

namespace PassDo.Application.Favorites.Commands.AddFavorite;

public record AddFavoriteCommand(Guid ProductId) : IRequest<FavoriteDto>;

public class AddFavoriteCommandValidator : AbstractValidator<AddFavoriteCommand>
{
    public AddFavoriteCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}

public class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand, FavoriteDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddFavoriteCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<FavoriteDto> Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var userId = _currentUserService.UserId.Value;

        var product = await _context.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.ProductId);
        }

        if (product.SellerId == userId)
        {
            throw new ConflictException("You cannot favorite your own product.");
        }

        if (!ProductStatusTransitions.IsPubliclyListable(product.Status))
        {
            throw new ConflictException("This product cannot be favorited.");
        }

        var existing = await _context.Favorites
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == request.ProductId, cancellationToken);

        if (existing is not null)
        {
            throw new ConflictException("Product is already in favorites.");
        }

        var favorite = new Favorite
        {
            UserId = userId,
            ProductId = product.Id
        };

        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync(cancellationToken);

        return new FavoriteDto
        {
            Id = favorite.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            SellingPrice = product.SellingPrice,
            Status = product.Status.ToString(),
            Condition = product.Condition.ToString(),
            Location = product.Location,
            PrimaryImageUrl = product.Images
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .Select(x => x.Url)
                .FirstOrDefault(),
            CreatedAt = favorite.CreatedAt
        };
    }
}
