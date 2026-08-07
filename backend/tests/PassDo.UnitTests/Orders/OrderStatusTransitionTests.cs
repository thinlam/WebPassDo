using FluentAssertions;
using PassDo.Application.Orders;
using PassDo.Domain.Enums;

namespace PassDo.UnitTests.Orders;

public class OrderStatusTransitionTests
{
    [Theory]
    [InlineData(OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.DeliveryFailed, true)]
    [InlineData(OrderStatus.Returned, true)]
    [InlineData(OrderStatus.Refunded, true)]
    [InlineData(OrderStatus.Completed, true)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Shipping, false)]
    [InlineData(OrderStatus.PendingSellerConfirmation, false)]
    public void IsTerminal_ReturnsExpected(OrderStatus s, bool expected)
        => OrderStatusTransitions.IsTerminal(s).Should().Be(expected);

    [Theory]
    [InlineData(OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Completed, false)]
    [InlineData(OrderStatus.Shipping, false)]
    public void CanBuyerConfirmComplete_ReturnsExpected(OrderStatus s, bool expected)
        => OrderStatusTransitions.CanBuyerConfirmComplete(s).Should().Be(expected);

    [Fact]
    public void IsActive_IsInverse_Of_IsTerminal()
    {
        foreach (OrderStatus s in Enum.GetValues<OrderStatus>())
        {
            OrderStatusTransitions.IsActive(s).Should().Be(!OrderStatusTransitions.IsTerminal(s));
        }
    }

    [Theory]
    [InlineData(OrderStatus.PendingSellerConfirmation, true)]
    [InlineData(OrderStatus.Preparing, true)]
    [InlineData(OrderStatus.ReadyForShipment, true)]
    [InlineData(OrderStatus.Shipping, true)]
    [InlineData(OrderStatus.AwaitingPayment, true)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Completed, false)]
    public void IsProductReserving_ReturnsExpected(OrderStatus s, bool expected)
        => OrderStatusTransitions.IsProductReserving(s).Should().Be(expected);
}

