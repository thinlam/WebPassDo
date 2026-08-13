using PassDo.Domain.Enums;

namespace PassDo.Api.Contracts.Orders;

public record PreviewOrderRequest(
    Guid ProductId,
    int Quantity,
    Guid? ShippingAddressId,
    DeliverySpeed DeliverySpeed,
    PaymentMethod PaymentMethod);

public record CreateOrderRequest(
    Guid ProductId,
    int Quantity,
    Guid ShippingAddressId,
    DeliverySpeed DeliverySpeed,
    PaymentMethod PaymentMethod,
    string? Note);

public record UploadPaymentProofRequest(string ProofImageUrl);
public record NoteRequest(string? Note);
public record ReasonRequest(string Reason);
public record RejectOrderRequest(OrderRejectReason ReasonCode, string? ReasonNote);

public record HandOverRequest(
    string DeliveryPersonName,
    string DeliveryPersonPhone,
    string DeliveryCompany,
    string? VehicleNumber,
    string? TrackingCode,
    string? DeliveryNote,
    DateTime? EstimatedDeliveryFrom,
    DateTime? EstimatedDeliveryTo);
