using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Options;
using PassDo.Application.Orders.DTOs;
using PassDo.Application.Orders.Helpers;
using PassDo.Application.Orders.Mappings;
using PassDo.Domain.Enums;

namespace PassDo.Application.Orders.Commands.PreviewOrder;

public record PreviewOrderCommand(
    Guid ProductId,
    int Quantity,
    Guid? ShippingAddressId,
    DeliverySpeed DeliverySpeed,
    PaymentMethod PaymentMethod) : IRequest<OrderPreviewDto>;

public class PreviewOrderCommandValidator : AbstractValidator<PreviewOrderCommand>
{
    public PreviewOrderCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.DeliverySpeed).IsInEnum();
        RuleFor(x => x.PaymentMethod).IsInEnum();
    }
}

public class PreviewOrderCommandHandler : IRequestHandler<PreviewOrderCommand, OrderPreviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IShippingCalculator _shippingCalculator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PreviewOrderCommandHandler(
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

    public async Task<OrderPreviewDto> Handle(PreviewOrderCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var buyerId = _currentUserService.UserId.Value;

        var product = await _context.Products
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.BankAccount)
            .Include(x => x.PickupAddress)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        var speeds = OrderHelpers.ParseDeliverySpeeds(product.AllowedDeliverySpeeds);
        var payments = OrderHelpers.AllowedPaymentMethods(product.AcceptedPaymentOption);
        var productTotal = product.SellingPrice * request.Quantity;
        var utcNow = _dateTimeProvider.UtcNow;

        var pickup = product.PickupAddress
            ?? await _context.UserAddresses
                .AsNoTracking()
                .Where(x => x.UserId == product.SellerId)
                .OrderByDescending(x => x.IsDefault)
                .FirstOrDefaultAsync(cancellationToken);

        string pickupProvince = pickup?.Province ?? string.Empty;
        string pickupDistrict = pickup?.District ?? string.Empty;
        string deliveryProvince = string.Empty;
        string deliveryDistrict = string.Empty;

        if (request.ShippingAddressId.HasValue)
        {
            var addr = await _context.UserAddresses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.ShippingAddressId && x.UserId == buyerId, cancellationToken);
            if (addr is not null)
            {
                deliveryProvince = addr.Province;
                deliveryDistrict = addr.District ?? string.Empty;
            }
        }

        ShippingQuote quote;
        if (!string.IsNullOrWhiteSpace(deliveryProvince) && !string.IsNullOrWhiteSpace(pickupProvince))
        {
            quote = _shippingCalculator.CalculateForAddresses(
                pickupProvince, pickupDistrict,
                deliveryProvince, deliveryDistrict,
                request.DeliverySpeed, utcNow);
        }
        else
        {
            var fee = _shippingCalculator.GetShippingFee(request.DeliverySpeed);
            var eta = _shippingCalculator.CalculateEta(request.DeliverySpeed, utcNow);
            quote = new ShippingQuote
            {
                ShippingFee = fee,
                EstimatedDeliveryFrom = eta.From,
                EstimatedDeliveryTo = eta.To,
                SuggestedSpeed = request.DeliverySpeed
            };
        }

        return new OrderPreviewDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductImageUrl = product.Images
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .Select(x => x.Url)
                .FirstOrDefault(),
            UnitPrice = product.SellingPrice,
            Quantity = request.Quantity,
            ProductTotal = productTotal,
            ShippingFee = quote.ShippingFee,
            GrandTotal = productTotal + quote.ShippingFee,
            DeliverySpeed = request.DeliverySpeed.ToString(),
            PaymentMethod = request.PaymentMethod.ToString(),
            EtaNote = "Thời gian nhận hàng chính thức được tính từ lúc người bán bàn giao hàng cho đơn vị vận chuyển.",
            EstimatedDeliveryFromPreview = quote.EstimatedDeliveryFrom,
            EstimatedDeliveryToPreview = quote.EstimatedDeliveryTo,
            AllowedDeliverySpeeds = speeds.Select(x => x.ToString()).ToList(),
            AllowedPaymentMethods = payments.Select(x => x.ToString()).ToList(),
            SellerBankAccount = product.BankAccount is null ? null : new OrderBankSnapshotDto
            {
                BankName = product.BankAccount.BankName,
                AccountHolderName = product.BankAccount.AccountHolderName,
                Branch = product.BankAccount.Branch,
                AccountNumber = product.BankAccount.AccountNumber,
                AccountNumberMasked = OrderMapper.MaskAccountNumber(product.BankAccount.AccountNumber)
            }
        };
    }
}
