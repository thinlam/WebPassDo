using PassDo.Domain.Common;
using PassDo.Domain.Enums;

namespace PassDo.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public ProductCondition Condition { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public string Location { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public Guid CategoryId { get; set; }
    public Guid SellerId { get; set; }
    public Guid? PickupAddressId { get; set; }
    public Guid? BankAccountId { get; set; }
    public AcceptedPaymentOption AcceptedPaymentOption { get; set; } = AcceptedPaymentOption.Both;
    /// <summary>Comma-separated DeliverySpeed values, e.g. "Express,Standard,Intercity".</summary>
    public string AllowedDeliverySpeeds { get; set; } = "Standard,Intercity";
    public int ViewCount { get; set; }

    public Category Category { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public UserAddress? PickupAddress { get; set; }
    public UserBankAccount? BankAccount { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
