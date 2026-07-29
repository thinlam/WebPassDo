namespace PassDo.Domain.Enums;

public enum OrderStatus
{
    AwaitingPayment = 0,
    PendingSellerConfirmation = 1,
    Preparing = 2,
    ReadyForShipment = 3,
    Shipping = 4,
    Delivered = 5,
    Cancelled = 6,
    DeliveryFailed = 7,
    Returned = 8,
    Refunded = 9,
    Completed = 10
}
