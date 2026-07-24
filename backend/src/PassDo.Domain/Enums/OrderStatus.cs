namespace PassDo.Domain.Enums;

public enum OrderStatus
{
    AwaitingPayment = 0,
    PendingConfirmation = 1,
    AwaitingPreparation = 2,
    AwaitingHandover = 3,
    Shipping = 4,
    Delivered = 5,
    Cancelled = 6,
    DeliveryFailed = 7,
    Returned = 8,
    Refunded = 9
}
