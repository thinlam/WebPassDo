namespace PassDo.Domain.Enums;

public enum OrderStatus
{
    AwaitingPayment = 0,
    PendingConfirmation = 1,
    AwaitingPickup = 2,
    Shipping = 3,
    Delivered = 4,
    Cancelled = 5,
    DeliveryFailed = 6,
    Returned = 7,
    Refunded = 8
}
