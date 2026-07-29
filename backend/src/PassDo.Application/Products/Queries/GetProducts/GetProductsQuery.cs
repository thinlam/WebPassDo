using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Models;
using PassDo.Application.Products.DTOs;
using PassDo.Application.Products.Mappings;
using PassDo.Domain.Enums;

namespace PassDo.Application.Products.Queries.GetProducts;

public record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Keyword = null,
    Guid? CategoryId = null,
    ProductCondition? Condition = null,
    ProductStatus? Status = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Location = null,
    string? SortBy = "createdAt",
    string? SortDirection = "desc") : IRequest<PagedResult<ProductListItemDto>>;

public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("minPrice must be less than or equal to maxPrice.");
        RuleFor(x => x.Condition).IsInEnum().When(x => x.Condition.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProductListItemDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .AsQueryable();

        // Public listing defaults to Available unless a specific status is requested.
        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }
        else
        {
            query = query.Where(x => x.Status == ProductStatus.Active);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                x.Description.Contains(keyword));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        }

        if (request.Condition.HasValue)
        {
            query = query.Where(x => x.Condition == request.Condition.Value);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(x => x.SellingPrice >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(x => x.SellingPrice <= request.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            var location = request.Location.Trim();
            query = query.Where(x => x.Location.Contains(location));
        }

        var sortBy = request.SortBy?.Trim().ToLowerInvariant() ?? "createdat";
        var sortDesc = !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        query = (sortBy, sortDesc) switch
        {
            ("sellingprice" or "price", true) => query.OrderByDescending(x => x.SellingPrice),
            ("sellingprice" or "price", false) => query.OrderBy(x => x.SellingPrice),
            ("name", true) => query.OrderByDescending(x => x.Name),
            ("name", false) => query.OrderBy(x => x.Name),
            (_, true) => query.OrderByDescending(x => x.CreatedAt),
            (_, false) => query.OrderBy(x => x.CreatedAt)
        };

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
