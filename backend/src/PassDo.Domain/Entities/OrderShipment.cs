using PassDo.Domain.Common;
using PassDo.Domain.Enums;

namespace PassDo.Domain.Entities;

public class OrderShipment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid? ShipperId { get; set; }
    public string? CarrierName { get; set; }
    public string? TrackingCode { get; set; }
    public DeliverySpeed DeliverySpeed { get; set; }
    public string SenderCity { get; set; } = string.Empty;
    public string ReceiverCity { get; set; } = string.Empty;
    public decimal ShippingFee { get; set; }
    public DateTime? EstimatedDeliveryFrom { get; set; }
    public DateTime? EstimatedDeliveryTo { get; set; }
    public DateTime? SellerHandedOverAt { get; set; }
    public DateTime? ShipperReceivedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public Guid? PreparedByUserId { get; set; }
    public Guid? PickedUpConfirmedByUserId { get; set; }
    public Guid? DeliveredConfirmedByUserId { get; set; }
    public string? DeliveryNote { get; set; }

    public Order Order { get; set; } = null!;
    public User? Shipper { get; set; }
}
