namespace PassDo.Application.Products.DTOs;

public class ProductImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid SellerId { get; set; }
    public string? SellerName { get; set; }
    public string? SellerPhoneNumber { get; set; }
    public bool SellerIsOnline { get; set; }
    public DateTime? SellerLastSeenAt { get; set; }
    public Guid? PickupAddressId { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? PickupAddressFull { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumberMasked { get; set; }
    public string? BankAccountHolderName { get; set; }
    public string AcceptedPaymentOption { get; set; } = string.Empty;
    public IReadOnlyList<string> AllowedDeliverySpeeds { get; set; } = Array.Empty<string>();
    public bool HasActiveOrders { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyList<ProductImageDto> Images { get; set; } = Array.Empty<ProductImageDto>();
}

public class ProductListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid SellerId { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
