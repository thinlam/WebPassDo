using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Orders.Mappings;
using PassDo.Application.Presence;
using PassDo.Application.Products;
using PassDo.Application.Products.DTOs;
using PassDo.Application.Products.Mappings;
using PassDo.Domain.Constants;
using PassDo.Domain.Enums;

namespace PassDo.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetProductByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .Include(x => x.PickupAddress)
            .Include(x => x.BankAccount)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        var isOwner = _currentUserService.UserId == product.SellerId;
        var isAdmin = string.Equals(_currentUserService.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isAdmin && !ProductStatusTransitions.IsPubliclyListable(product.Status))
        {
            throw new NotFoundException("Product", request.Id);
        }

        // Count a view for public browsing (skip seller viewing their own listing).
        if (!isOwner && ProductStatusTransitions.IsPubliclyListable(product.Status))
        {
            var tracked = await _context.Products.FirstAsync(x => x.Id == product.Id, cancellationToken);
            tracked.ViewCount += 1;
            await _context.SaveChangesAsync(cancellationToken);
            product.ViewCount = tracked.ViewCount;
        }

        var seller = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == product.SellerId, cancellationToken);

        var hasActiveOrders = await _context.Orders.AnyAsync(
            x => x.ProductId == product.Id
                 && PassDo.Application.Orders.DTOs.OrderStatusGroups.ActiveProcessing.Contains(x.Status),
            cancellationToken);

        var dto = ProductMapper.ToDto(product, hasActiveOrders);
        dto.SellerName = seller?.FullName;
        dto.SellerIsOnline = PresenceRules.IsOnline(seller?.LastSeenAt, DateTime.UtcNow);
        dto.SellerLastSeenAt = seller?.LastSeenAt;

        if (isOwner || isAdmin)
        {
            dto.SellerPhoneNumber = seller?.PhoneNumber;
            if (product.PickupAddress is not null)
            {
                dto.PickupAddressFull = OrderMapper.FormatAddress(
                    product.PickupAddress.StreetAddress,
                    product.PickupAddress.Ward,
                    product.PickupAddress.District,
                    product.PickupAddress.Province);
            }

            if (product.BankAccount is not null)
            {
                dto.BankName = product.BankAccount.BankName;
                dto.BankAccountNumberMasked = OrderMapper.MaskAccountNumber(product.BankAccount.AccountNumber);
                dto.BankAccountHolderName = product.BankAccount.AccountHolderName;
            }
        }

        return dto;
    }
}
