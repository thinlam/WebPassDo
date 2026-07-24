using PassDo.Domain.Common;
using PassDo.Domain.Enums;

namespace PassDo.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderCode { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public Guid? ShipperId { get; set; }

    public decimal ProductTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal GrandTotal { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PendingConfirmation;
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public DeliverySpeed DeliverySpeed { get; set; }

    public string? Note { get; set; }
    public string? CancellationReason { get; set; }

    public DateTime? EstimatedDeliveryFrom { get; set; }
    public DateTime? EstimatedDeliveryTo { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? PreparedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Buyer shipping address snapshot
    public string ShippingRecipientName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingProvince { get; set; } = string.Empty;
    public string ShippingDistrict { get; set; } = string.Empty;
    public string ShippingWard { get; set; } = string.Empty;
    public string ShippingStreetAddress { get; set; } = string.Empty;
    public string? ShippingAddressNote { get; set; }

    // Seller pickup address snapshot
    public string PickupRecipientName { get; set; } = string.Empty;
    public string PickupPhone { get; set; } = string.Empty;
    public string PickupProvince { get; set; } = string.Empty;
    public string PickupDistrict { get; set; } = string.Empty;
    public string PickupWard { get; set; } = string.Empty;
    public string PickupStreetAddress { get; set; } = string.Empty;

    // Bank account snapshot at order time
    public string? BankNameSnapshot { get; set; }
    public string? BankAccountNumberSnapshot { get; set; }
    public string? BankAccountHolderSnapshot { get; set; }
    public string? BankBranchSnapshot { get; set; }

    // Legacy field kept for migration compatibility during transition
    public decimal Price { get; set; }

    public Product Product { get; set; } = null!;
    public User Buyer { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public OrderPayment? Payment { get; set; }
    public OrderShipment? Shipment { get; set; }
    public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
}
