using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Models;
using PassDo.Application.Orders.DTOs;
using PassDo.Application.Orders.Mappings;
using PassDo.Domain.Enums;

namespace PassDo.Application.Orders.Queries.GetMySales;

public record GetMySalesQuery(int Page, int PageSize, OrderStatus? Status) : IRequest<PagedResult<OrderListItemDto>>;

public class GetMySalesQueryHandler : IRequestHandler<GetMySalesQuery, PagedResult<OrderListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMySalesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<OrderListItemDto>> Handle(GetMySalesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _context.Orders.AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .Include(x => x.Shipper)
            .Where(x => x.SellerId == _currentUser.UserId);

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<OrderListItemDto>.Create(
            items.Select(OrderMapper.ToListItemDto).ToList(),
            page,
            pageSize,
            total);
    }
}
