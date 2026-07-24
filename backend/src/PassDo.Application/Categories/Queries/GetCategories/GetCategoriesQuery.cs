using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Categories.DTOs;
using PassDo.Application.Categories.Mappings;
using PassDo.Application.Common.Interfaces;

namespace PassDo.Application.Categories.Queries.GetCategories;

public record GetCategoriesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Categories.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var categories = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(CategoryMapper.ToDto).ToList();
    }
}
