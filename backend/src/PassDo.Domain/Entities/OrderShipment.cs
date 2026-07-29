using PassDo.Domain.Common;
using PassDo.Domain.Enums;

namespace PassDo.Domain.Entities;

public class OrderShipment : BaseEntity
{
    public Guid OrderId { get; set; }
    public string? DeliveryPersonName { get; set; }
    public string? DeliveryPersonPhone { get; set; }
    public string? DeliveryCompany { get; set; }
    public string? VehicleNumber { get; set; }
    public string? TrackingCode { get; set; }
    public DeliverySpeed DeliverySpeed { get; set; }
    public string SenderCity { get; set; } = string.Empty;
    public string ReceiverCity { get; set; } = string.Empty;
    public string? SenderDistrict { get; set; }
    public string? ReceiverDistrict { get; set; }
    public decimal ShippingFee { get; set; }
    public bool IsInnerCity { get; set; }
    public DateTime? EstimatedDeliveryFrom { get; set; }
    public DateTime? EstimatedDeliveryTo { get; set; }
    public DateTime? SellerHandedOverAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public Guid? PreparedByUserId { get; set; }
    public Guid? HandedOverByUserId { get; set; }
    public Guid? DeliveredConfirmedByUserId { get; set; }
    public string? DeliveryNote { get; set; }

    public string? CarrierName { get; set; }

    public Order Order { get; set; } = null!;
}
