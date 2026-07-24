using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Options;
using PassDo.Domain.Enums;

namespace PassDo.Application.Shipping;

public record CalculateShippingCommand(
    Guid ProductId,
    Guid? PickupAddressId,
    Guid DeliveryAddressId,
    DeliverySpeed? DeliverySpeed) : IRequest<ShippingQuoteDto>;

public class CalculateShippingCommandValidator : AbstractValidator<CalculateShippingCommand>
{
    public CalculateShippingCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.DeliveryAddressId).NotEmpty();
    }
}

public class ShippingQuoteDto
{
    public bool IsInnerCity { get; set; }
    public decimal ShippingFee { get; set; }
    public DateTime EstimatedDeliveryFrom { get; set; }
    public DateTime EstimatedDeliveryTo { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SuggestedSpeed { get; set; } = string.Empty;
}

public class CalculateShippingCommandHandler : IRequestHandler<CalculateShippingCommand, ShippingQuoteDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IShippingCalculator _shippingCalculator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CalculateShippingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IShippingCalculator shippingCalculator,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _shippingCalculator = shippingCalculator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ShippingQuoteDto> Handle(CalculateShippingCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var product = await _context.Products
            .AsNoTracking()
            .Include(x => x.PickupAddress)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        var pickup = request.PickupAddressId.HasValue
            ? await _context.UserAddresses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.PickupAddressId, cancellationToken)
            : product.PickupAddress
              ?? await _context.UserAddresses.AsNoTracking()
                  .Where(x => x.UserId == product.SellerId)
                  .OrderByDescending(x => x.IsDefault)
                  .FirstOrDefaultAsync(cancellationToken);

        if (pickup is null)
        {
            throw new ConflictException("Seller has not configured a pickup address.");
        }

        var delivery = await _context.UserAddresses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.DeliveryAddressId && x.UserId == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("UserAddress", request.DeliveryAddressId);

        var quote = _shippingCalculator.CalculateForAddresses(
            pickup.Province, pickup.District ?? string.Empty,
            delivery.Province, delivery.District ?? string.Empty,
            request.DeliverySpeed,
            _dateTimeProvider.UtcNow);

        return new ShippingQuoteDto
        {
            IsInnerCity = quote.IsInnerCity,
            ShippingFee = quote.ShippingFee,
            EstimatedDeliveryFrom = quote.EstimatedDeliveryFrom,
            EstimatedDeliveryTo = quote.EstimatedDeliveryTo,
            Description = quote.Description,
            SuggestedSpeed = quote.SuggestedSpeed.ToString()
        };
    }
}
