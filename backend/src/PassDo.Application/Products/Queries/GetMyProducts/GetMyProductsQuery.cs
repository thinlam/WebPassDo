using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Models;
using PassDo.Application.Products.DTOs;
using PassDo.Application.Products.Mappings;
using PassDo.Domain.Enums;

namespace PassDo.Application.Products.Queries.GetMyProducts;

public record GetMyProductsQuery(
    int Page = 1,
    int PageSize = 20,
    ProductStatus? Status = null) : IRequest<PagedResult<ProductListItemDto>>;

public class GetMyProductsQueryValidator : AbstractValidator<GetMyProductsQuery>
{
    public GetMyProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}

public class GetMyProductsQueryHandler : IRequestHandler<GetMyProductsQuery, PagedResult<ProductListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyProductsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<ProductListItemDto>> Handle(GetMyProductsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var query = _context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Where(x => x.SellerId == _currentUserService.UserId.Value);

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<ProductListItemDto>.Create(
            items.Select(ProductMapper.ToListItemDto).ToList(),
            request.Page,
            request.PageSize,
            totalItems);
    }
}
