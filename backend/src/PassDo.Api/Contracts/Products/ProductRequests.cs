using PassDo.Domain.Enums;

namespace PassDo.Api.Contracts.Products;

public record CreateProductRequest(
    string Name,
    string Description,
    decimal OriginalPrice,
    decimal SellingPrice,
    ProductCondition Condition,
    Guid CategoryId,
    string Location,
    int Quantity = 1,
    Guid? PickupAddressId = null,
    Guid? BankAccountId = null,
    AcceptedPaymentOption AcceptedPaymentOption = AcceptedPaymentOption.Both,
    IReadOnlyList<DeliverySpeed>? AllowedDeliverySpeeds = null,
    ProductStatus? Status = null);

public record UpdateProductRequest(
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
    ProductStatus? Status = null);

public record UpdateProductStatusRequest(ProductStatus Status);
