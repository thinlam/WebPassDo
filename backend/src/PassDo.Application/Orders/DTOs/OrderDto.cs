using PassDo.Domain.Enums;

namespace PassDo.Application.Orders.DTOs;

public class OrderListItemDto
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal ProductTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string DeliverySpeed { get; set; } = string.Empty;
    public DateTime? EstimatedDeliveryFrom { get; set; }
    public DateTime? EstimatedDeliveryTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid BuyerId { get; set; }
    public string? BuyerName { get; set; }
    public Guid SellerId { get; set; }
    public string? SellerName { get; set; }
    public Guid? ShipperId { get; set; }
    public string? ShipperName { get; set; }
}

public class OrderDetailDto : OrderListItemDto
{
    public string? Note { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? PreparedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public OrderPartyDto? Seller { get; set; }
    public OrderPartyDto? Buyer { get; set; }
    public OrderPartyDto? Shipper { get; set; }
    public OrderAddressDto? ShippingAddress { get; set; }
    public OrderAddressDto? PickupAddress { get; set; }
    public OrderPaymentDto? Payment { get; set; }
    public OrderShipmentDto? Shipment { get; set; }
    public OrderBankSnapshotDto? SellerBankAccount { get; set; }
    public IReadOnlyList<OrderItemDto> Items { get; set; } = Array.Empty<OrderItemDto>();
    public IReadOnlyList<OrderStatusHistoryDto> StatusHistory { get; set; } = Array.Empty<OrderStatusHistoryDto>();
}

public class OrderPartyDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class OrderAddressDto
{
    public string RecipientName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string FullAddress { get; set; } = string.Empty;
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderPaymentDto
{
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? TransferContent { get; set; }
    public string? ProofImageUrl { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public class OrderShipmentDto
{
    public string? CarrierName { get; set; }
    public string? TrackingCode { get; set; }
    public string DeliverySpeed { get; set; } = string.Empty;
    public string SenderCity { get; set; } = string.Empty;
    public string ReceiverCity { get; set; } = string.Empty;
    public decimal ShippingFee { get; set; }
    public DateTime? EstimatedDeliveryFrom { get; set; }
    public DateTime? EstimatedDeliveryTo { get; set; }
    public DateTime? SellerHandedOverAt { get; set; }
    public DateTime? ShipperReceivedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryNote { get; set; }
    public Guid? ShipperId { get; set; }
    public string? ShipperName { get; set; }
    public string? ShipperPhone { get; set; }
}

public class OrderBankSnapshotDto
{
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountNumberMasked { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string? Branch { get; set; }
}

public class OrderStatusHistoryDto
{
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? ChangedByRole { get; set; }
    public string? ChangedByName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderPreviewDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal ProductTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal GrandTotal { get; set; }
    public string DeliverySpeed { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string EtaNote { get; set; } = string.Empty;
    public DateTime? EstimatedDeliveryFromPreview { get; set; }
    public DateTime? EstimatedDeliveryToPreview { get; set; }
    public OrderBankSnapshotDto? SellerBankAccount { get; set; }
    public IReadOnlyList<string> AllowedDeliverySpeeds { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AllowedPaymentMethods { get; set; } = Array.Empty<string>();
}

public static class OrderStatusGroups
{
    public static readonly OrderStatus[] ActiveProcessing =
    [
        OrderStatus.AwaitingPayment,
        OrderStatus.PendingConfirmation,
        OrderStatus.AwaitingPickup,
        OrderStatus.Shipping
    ];
}
