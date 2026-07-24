using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Models;
using PassDo.Application.Favorites.DTOs;

namespace PassDo.Application.Favorites.Queries.GetFavorites;

public record GetFavoritesQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<FavoriteDto>>;

public class GetFavoritesQueryValidator : AbstractValidator<GetFavoritesQuery>
{
    public GetFavoritesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetFavoritesQueryHandler : IRequestHandler<GetFavoritesQuery, PagedResult<FavoriteDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFavoritesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<FavoriteDto>> Handle(GetFavoritesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var query = _context.Favorites
            .AsNoTracking()
            .Include(x => x.Product)
                .ThenInclude(p => p.Images)
            .Where(x => x.UserId == _currentUserService.UserId)
            .OrderByDescending(x => x.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new FavoriteDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductName = x.Product.Name,
            SellingPrice = x.Product.SellingPrice,
            Status = x.Product.Status.ToString(),
            Condition = x.Product.Condition.ToString(),
            Location = x.Product.Location,
            PrimaryImageUrl = x.Product.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.DisplayOrder)
                .Select(i => i.Url)
                .FirstOrDefault(),
            CreatedAt = x.CreatedAt
        }).ToList();

        return PagedResult<FavoriteDto>.Create(dtos, request.Page, request.PageSize, totalItems);
    }
}
