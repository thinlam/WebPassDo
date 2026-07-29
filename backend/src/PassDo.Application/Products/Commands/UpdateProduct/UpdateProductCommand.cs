using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Orders.DTOs;
using PassDo.Application.Orders.Helpers;
using PassDo.Application.Products.DTOs;
using PassDo.Application.Products.Mappings;
using PassDo.Domain.Constants;
using PassDo.Domain.Enums;

namespace PassDo.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal OriginalPrice,
    decimal SellingPrice,
    ProductCondition Condition,
    Guid CategoryId,
    string Location,
    int Quantity,
    Guid? PickupAddressId,
    Guid? BankAccountId,
    AcceptedPaymentOption AcceptedPaymentOption,
    IReadOnlyList<DeliverySpeed> AllowedDeliverySpeeds,
    ProductStatus? Status) : IRequest<ProductDto>;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.OriginalPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice).GreaterThan(0);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Location).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Condition).IsInEnum();
        RuleFor(x => x.AcceptedPaymentOption).IsInEnum();
        RuleFor(x => x.AllowedDeliverySpeeds).NotEmpty();
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProductCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var product = await _context.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Product", request.Id);

        EnsureCanModify(product.SellerId);

        if (product.Status == ProductStatus.Sold)
        {
            throw new ConflictException("Cannot update a sold product.");
        }

        var hasActiveOrders = await _context.Orders.AnyAsync(
            x => x.ProductId == product.Id && OrderStatusGroups.ActiveProcessing.Contains(x.Status),
            cancellationToken);

        if (hasActiveOrders && (request.SellingPrice != product.SellingPrice || request.OriginalPrice != product.OriginalPrice))
        {
            throw new ConflictException("Cannot change product price while there are active orders.");
        }

        var categoryExists = await _context.Categories
            .AnyAsync(x => x.Id == request.CategoryId && x.IsActive, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException("Category", request.CategoryId);
        }

        if (request.PickupAddressId.HasValue)
        {
            var ok = await _context.UserAddresses.AnyAsync(
                x => x.Id == request.PickupAddressId && x.UserId == product.SellerId, cancellationToken);
            if (!ok) throw new NotFoundException("UserAddress", request.PickupAddressId.Value);
        }

        if (request.BankAccountId.HasValue)
        {
            var ok = await _context.UserBankAccounts.AnyAsync(
                x => x.Id == request.BankAccountId && x.UserId == product.SellerId, cancellationToken);
            if (!ok) throw new NotFoundException("UserBankAccount", request.BankAccountId.Value);
        }

        if (request.AcceptedPaymentOption is AcceptedPaymentOption.BankTransfer or AcceptedPaymentOption.Both
            && request.BankAccountId is null)
        {
            throw new ConflictException("Bank account is required when accepting bank transfer.");
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.OriginalPrice = request.OriginalPrice;
        product.SellingPrice = request.SellingPrice;
        product.Condition = request.Condition;
        product.CategoryId = request.CategoryId;
        product.Location = request.Location.Trim();
        product.Quantity = request.Quantity;
        product.PickupAddressId = request.PickupAddressId;
        product.BankAccountId = request.BankAccountId;
        product.AcceptedPaymentOption = request.AcceptedPaymentOption;
        product.AllowedDeliverySpeeds = OrderHelpers.JoinDeliverySpeeds(request.AllowedDeliverySpeeds);

        if (request.Status.HasValue
            && request.Status is ProductStatus.Draft or ProductStatus.Active or ProductStatus.Hidden)
        {
            product.Status = request.Status.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var updated = await _context.Products.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .FirstAsync(x => x.Id == product.Id, cancellationToken);

        var seller = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == updated.SellerId, cancellationToken);
        var dto = ProductMapper.ToDto(updated, hasActiveOrders);
        dto.SellerName = seller?.FullName;
        return dto;
    }

    private void EnsureCanModify(Guid sellerId)
    {
        var isOwner = _currentUserService.UserId == sellerId;
        var isAdmin = string.Equals(_currentUserService.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        if (!isOwner && !isAdmin)
        {
            throw new ForbiddenException("You can only update your own products.");
        }
    }
}
