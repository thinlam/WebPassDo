using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Orders.DTOs;
using PassDo.Application.Orders.Mappings;
using PassDo.Domain.Constants;

namespace PassDo.Application.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailDto>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetOrderByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<OrderDetailDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Payment)
            .Include(x => x.Shipment)
            .Include(x => x.StatusHistories).ThenInclude(x => x.ChangedByUser)
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Order", request.Id);

        var isAdmin = string.Equals(_currentUser.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        var isParticipant = order.BuyerId == _currentUser.UserId
            || order.SellerId == _currentUser.UserId
            || isAdmin;

        if (!isParticipant)
        {
            throw new ForbiddenException("You are not allowed to view this order.");
        }

        var includeBank = order.BuyerId == _currentUser.UserId
            || order.SellerId == _currentUser.UserId
            || isAdmin;

        return OrderMapper.ToDetailDto(order, includeSensitiveContact: true, includeFullBankAccount: includeBank);
    }
}
