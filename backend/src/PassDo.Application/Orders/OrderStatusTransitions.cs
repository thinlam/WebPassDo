using PassDo.Domain.Enums;

namespace PassDo.Application.Orders;

public static class OrderStatusTransitions
{
    public static bool IsTerminal(OrderStatus s) =>
        s is OrderStatus.Cancelled
            or OrderStatus.DeliveryFailed
            or OrderStatus.Returned
            or OrderStatus.Refunded
            or OrderStatus.Completed;

    public static bool IsActive(OrderStatus s) => !IsTerminal(s);

    public static bool CanBuyerConfirmComplete(OrderStatus s) =>
        s == OrderStatus.Delivered;

    public static bool IsProductReserving(OrderStatus s) =>
        s is OrderStatus.AwaitingPayment
            or OrderStatus.PendingSellerConfirmation
            or OrderStatus.Preparing
            or OrderStatus.ReadyForShipment
            or OrderStatus.Shipping;
}

