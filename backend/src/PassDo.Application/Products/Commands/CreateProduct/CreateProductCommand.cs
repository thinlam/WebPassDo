using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Orders.DTOs;
using PassDo.Application.Orders.Helpers;
using PassDo.Application.Products.DTOs;
using PassDo.Application.Products.Mappings;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;

namespace PassDo.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
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

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.OriginalPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice).GreaterThan(0);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Location).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Condition).IsInEnum();
        RuleFor(x => x.AcceptedPaymentOption).IsInEnum();
        RuleFor(x => x.AllowedDeliverySpeeds).NotEmpty();
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateProductCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var sellerId = _currentUserService.UserId.Value;

        var categoryExists = await _context.Categories
            .AnyAsync(x => x.Id == request.CategoryId && x.IsActive, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException("Category", request.CategoryId);
        }

        if (request.PickupAddressId.HasValue)
        {
            var ok = await _context.UserAddresses.AnyAsync(
                x => x.Id == request.PickupAddressId && x.UserId == sellerId, cancellationToken);
            if (!ok) throw new NotFoundException("UserAddress", request.PickupAddressId.Value);
        }

        if (request.BankAccountId.HasValue)
        {
            var ok = await _context.UserBankAccounts.AnyAsync(
                x => x.Id == request.BankAccountId && x.UserId == sellerId, cancellationToken);
            if (!ok) throw new NotFoundException("UserBankAccount", request.BankAccountId.Value);
        }

        if (request.AcceptedPaymentOption is AcceptedPaymentOption.BankTransfer or AcceptedPaymentOption.Both
            && request.BankAccountId is null)
        {
            throw new ConflictException("Bank account is required when accepting bank transfer.");
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            OriginalPrice = request.OriginalPrice,
            SellingPrice = request.SellingPrice,
            Condition = request.Condition,
            Status = ProductStatus.Draft,
            Location = request.Location.Trim(),
            Quantity = request.Quantity,
            CategoryId = request.CategoryId,
            SellerId = sellerId,
            PickupAddressId = request.PickupAddressId,
            BankAccountId = request.BankAccountId,
            AcceptedPaymentOption = request.AcceptedPaymentOption,
            AllowedDeliverySpeeds = OrderHelpers.JoinDeliverySpeeds(request.AllowedDeliverySpeeds)
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        var created = await _context.Products.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .FirstAsync(x => x.Id == product.Id, cancellationToken);

        var seller = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == created.SellerId, cancellationToken);
        var dto = ProductMapper.ToDto(created);
        dto.SellerName = seller?.FullName;
        return dto;
    }
}
