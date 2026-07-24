using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Models;
using PassDo.Application.Orders.DTOs;
using PassDo.Application.Orders.Mappings;
using PassDo.Domain.Constants;
using PassDo.Domain.Enums;

namespace PassDo.Application.Orders.Queries.GetShipperOrders;

public record GetShipperOrdersQuery(int Page, int PageSize, OrderStatus? Status, bool AvailableOnly = false)
    : IRequest<PagedResult<OrderListItemDto>>;

public class GetShipperOrdersQueryHandler : IRequestHandler<GetShipperOrdersQuery, PagedResult<OrderListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetShipperOrdersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<OrderListItemDto>> Handle(GetShipperOrdersQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var isAdmin = string.Equals(_currentUser.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        var isShipper = string.Equals(_currentUser.Role, Roles.Shipper, StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !isShipper)
        {
            throw new ForbiddenException("Only shippers can view shipper orders.");
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _context.Orders.AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .Include(x => x.Shipper)
            .AsQueryable();

        if (request.AvailableOnly)
        {
            query = query.Where(x => x.Status == OrderStatus.AwaitingPickup && x.ShipperId == null && x.PreparedAt != null);
        }
        else if (!isAdmin)
        {
            query = query.Where(x => x.ShipperId == _currentUser.UserId);
        }
        else
        {
            query = query.Where(x => x.ShipperId != null || x.Status == OrderStatus.AwaitingPickup || x.Status == OrderStatus.Shipping);
        }

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
