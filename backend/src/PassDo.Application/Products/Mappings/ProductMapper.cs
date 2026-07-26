using PassDo.Application.Orders.Helpers;
using PassDo.Application.Products.DTOs;
using PassDo.Domain.Entities;

namespace PassDo.Application.Products.Mappings;

public static class ProductMapper
{
    public static ProductDto ToDto(Product product, bool hasActiveOrders = false) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        OriginalPrice = product.OriginalPrice,
        SellingPrice = product.SellingPrice,
        Condition = product.Condition.ToString(),
        Status = product.Status.ToString(),
        Location = product.Location,
        Quantity = product.Quantity,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name,
        SellerId = product.SellerId,
        SellerName = product.Seller?.FullName,
        PickupAddressId = product.PickupAddressId,
        BankAccountId = product.BankAccountId,
        AcceptedPaymentOption = product.AcceptedPaymentOption.ToString(),
        AllowedDeliverySpeeds = OrderHelpers.ParseDeliverySpeeds(product.AllowedDeliverySpeeds)
            .Select(x => x.ToString())
            .ToList(),
        HasActiveOrders = hasActiveOrders,
        ViewCount = product.ViewCount,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt,
        Images = product.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(ToImageDto)
            .ToList()
    };

    public static ProductListItemDto ToListItemDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        SellingPrice = product.SellingPrice,
        Condition = product.Condition.ToString(),
        Status = product.Status.ToString(),
        Location = product.Location,
        Quantity = product.Quantity,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name,
        SellerId = product.SellerId,
        PrimaryImageUrl = product.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(x => x.Url)
            .FirstOrDefault(),
        CreatedAt = product.CreatedAt
    };

    public static ProductImageDto ToImageDto(ProductImage image) => new()
    {
        Id = image.Id,
        Url = image.Url,
        FileName = image.FileName,
        IsPrimary = image.IsPrimary,
        DisplayOrder = image.DisplayOrder
    };
}
