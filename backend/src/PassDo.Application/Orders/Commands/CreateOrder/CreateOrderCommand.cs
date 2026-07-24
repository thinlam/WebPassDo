using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Options;
using PassDo.Application.Orders.DTOs;
using PassDo.Application.Orders.Helpers;
using PassDo.Application.Orders.Mappings;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;

namespace PassDo.Application.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid ProductId,
    int Quantity,
    Guid ShippingAddressId,
    DeliverySpeed DeliverySpeed,
    PaymentMethod PaymentMethod,
    string? Note) : IRequest<OrderDetailDto>;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.ShippingAddressId).NotEmpty();
        RuleFor(x => x.DeliverySpeed).IsInEnum();
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IShippingCalculator _shippingCalculator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateOrderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IShippingCalculator shippingCalculator,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _shippingCalculator = shippingCalculator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<OrderDetailDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var buyerId = _currentUserService.UserId.Value;

        var product = await _context.Products
            .Include(x => x.Images)
            .Include(x => x.PickupAddress)
            .Include(x => x.BankAccount)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        if (product.SellerId == buyerId)
        {
            throw new ConflictException("You cannot buy your own product.");
        }

        if (product.Status is not ProductStatus.Available)
        {
            throw new ConflictException($"Product is not available for purchase (status: {product.Status}).");
        }

        if (request.Quantity > product.Quantity)
        {
            throw new ConflictException("Requested quantity exceeds available stock.");
        }

        var allowedSpeeds = OrderHelpers.ParseDeliverySpeeds(product.AllowedDeliverySpeeds);
        if (!allowedSpeeds.Contains(request.DeliverySpeed))
        {
            throw new ConflictException("Selected delivery speed is not supported for this product.");
        }

        var allowedPayments = OrderHelpers.AllowedPaymentMethods(product.AcceptedPaymentOption);
        if (!allowedPayments.Contains(request.PaymentMethod))
        {
            throw new ConflictException("Selected payment method is not accepted by the seller.");
        }

        if (request.PaymentMethod == PaymentMethod.BankTransfer && product.BankAccount is null)
        {
            throw new ConflictException("Seller has not configured a bank account for transfers.");
        }

        var shippingAddress = await _context.UserAddresses
            .FirstOrDefaultAsync(x => x.Id == request.ShippingAddressId && x.UserId == buyerId, cancellationToken)
            ?? throw new NotFoundException("UserAddress", request.ShippingAddressId);

        UserAddress? pickup = product.PickupAddress;
        if (pickup is null)
        {
            pickup = await _context.UserAddresses
                .Where(x => x.UserId == product.SellerId)
                .OrderByDescending(x => x.IsDefault)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (pickup is null)
        {
            throw new ConflictException("Seller has not configured a pickup address.");
        }

        var hasActiveOrder = await _context.Orders.AnyAsync(
            x => x.ProductId == product.Id
                 && x.BuyerId == buyerId
                 && OrderStatusGroups.ActiveProcessing.Contains(x.Status),
            cancellationToken);

        if (hasActiveOrder)
        {
            throw new ConflictException("You already have an active order for this product.");
        }

        var buyer = await _context.Users.FirstAsync(x => x.Id == buyerId, cancellationToken);
        var seller = await _context.Users.FirstAsync(x => x.Id == product.SellerId, cancellationToken);

        var productTotal = product.SellingPrice * request.Quantity;
        var shippingFee = _shippingCalculator.GetShippingFee(request.DeliverySpeed);
        var grandTotal = productTotal + shippingFee;

        var initialStatus = request.PaymentMethod == PaymentMethod.BankTransfer
            ? OrderStatus.AwaitingPayment
            : OrderStatus.PendingConfirmation;

        var paymentStatus = request.PaymentMethod == PaymentMethod.BankTransfer
            ? PaymentStatus.Unpaid
            : PaymentStatus.Unpaid;

        var orderCode = OrderHelpers.GenerateOrderCode();
        while (await _context.Orders.AnyAsync(x => x.OrderCode == orderCode, cancellationToken))
        {
            orderCode = OrderHelpers.GenerateOrderCode();
        }

        var imageUrl = product.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(x => x.Url)
            .FirstOrDefault();

        var etaPreview = _shippingCalculator.CalculateEta(request.DeliverySpeed, _dateTimeProvider.UtcNow);

        var order = new Order
        {
            OrderCode = orderCode,
            ProductId = product.Id,
            BuyerId = buyerId,
            SellerId = product.SellerId,
            ProductTotal = productTotal,
            ShippingFee = shippingFee,
            GrandTotal = grandTotal,
            Price = productTotal,
            Status = initialStatus,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = paymentStatus,
            DeliverySpeed = request.DeliverySpeed,
            Note = request.Note?.Trim(),
            EstimatedDeliveryFrom = etaPreview.From,
            EstimatedDeliveryTo = etaPreview.To,
            ShippingRecipientName = shippingAddress.RecipientName,
            ShippingPhone = shippingAddress.PhoneNumber,
            ShippingProvince = shippingAddress.Province,
            ShippingDistrict = shippingAddress.District,
            ShippingWard = shippingAddress.Ward,
            ShippingStreetAddress = shippingAddress.StreetAddress,
            ShippingAddressNote = shippingAddress.Note,
            PickupRecipientName = pickup.RecipientName,
            PickupPhone = pickup.PhoneNumber,
            PickupProvince = pickup.Province,
            PickupDistrict = pickup.District,
            PickupWard = pickup.Ward,
            PickupStreetAddress = pickup.StreetAddress,
            BankNameSnapshot = product.BankAccount?.BankName,
            BankAccountNumberSnapshot = product.BankAccount?.AccountNumber,
            BankAccountHolderSnapshot = product.BankAccount?.AccountHolderName,
            BankBranchSnapshot = product.BankAccount?.Branch
        };

        order.Items.Add(new OrderItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductImageUrl = imageUrl,
            UnitPrice = product.SellingPrice,
            Quantity = request.Quantity,
            LineTotal = productTotal
        });

        order.Payment = new OrderPayment
        {
            Method = request.PaymentMethod,
            Status = paymentStatus,
            Amount = grandTotal,
            TransferContent = request.PaymentMethod == PaymentMethod.BankTransfer
                ? $"PASSDO {orderCode}"
                : null
        };

        order.Shipment = new OrderShipment
        {
            DeliverySpeed = request.DeliverySpeed,
            SenderCity = pickup.Province,
            ReceiverCity = shippingAddress.Province,
            ShippingFee = shippingFee,
            EstimatedDeliveryFrom = etaPreview.From,
            EstimatedDeliveryTo = etaPreview.To,
            CarrierName = "PassDo Shipper"
        };

        OrderHelpers.AddHistory(
            order,
            null,
            initialStatus,
            buyerId,
            _currentUserService.Role,
            request.PaymentMethod == PaymentMethod.BankTransfer
                ? "Người mua đã đặt hàng (chờ thanh toán chuyển khoản)."
                : "Người mua đã đặt hàng (COD).");

        product.Quantity -= request.Quantity;
        if (product.Quantity <= 0)
        {
            product.Status = ProductStatus.Reserved;
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        var created = await LoadOrder(order.Id, cancellationToken);
        return OrderMapper.ToDetailDto(created, includeSensitiveContact: true, includeFullBankAccount: true);
    }

    private async Task<Order> LoadOrder(Guid id, CancellationToken cancellationToken)
        => await _context.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Payment)
            .Include(x => x.Shipment)
            .Include(x => x.StatusHistories).ThenInclude(x => x.ChangedByUser)
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .Include(x => x.Shipper)
            .Include(x => x.Product).ThenInclude(x => x.Images)
            .FirstAsync(x => x.Id == id, cancellationToken);
}
