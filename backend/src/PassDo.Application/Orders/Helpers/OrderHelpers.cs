using PassDo.Domain.Entities;
using PassDo.Domain.Enums;

namespace PassDo.Application.Orders.Helpers;

public static class OrderHelpers
{
    public static string GenerateOrderCode()
    {
        var n = Random.Shared.Next(1, 999999);
        return $"DH{n:D6}";
    }

    public static IReadOnlyList<DeliverySpeed> ParseDeliverySpeeds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [DeliverySpeed.Standard, DeliverySpeed.Intercity];
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Enum.TryParse<DeliverySpeed>(x, true, out var s) ? s : (DeliverySpeed?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
    }

    public static string JoinDeliverySpeeds(IEnumerable<DeliverySpeed> speeds)
        => string.Join(",", speeds.Distinct());

    public static IReadOnlyList<PaymentMethod> AllowedPaymentMethods(AcceptedPaymentOption option) => option switch
    {
        AcceptedPaymentOption.BankTransfer => [PaymentMethod.BankTransfer],
        AcceptedPaymentOption.CashOnDelivery => [PaymentMethod.CashOnDelivery],
        _ => [PaymentMethod.BankTransfer, PaymentMethod.CashOnDelivery]
    };

    public static OrderStatusHistory CreateHistory(
        Guid orderId,
        OrderStatus? oldStatus,
        OrderStatus newStatus,
        Guid? userId,
        string? role,
        string? note)
        => new()
        {
            OrderId = orderId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = userId,
            ChangedByRole = role,
            Note = note
        };

    public static void AddHistory(
        Order order,
        OrderStatus? oldStatus,
        OrderStatus newStatus,
        Guid? userId,
        string? role,
        string? note)
    {
        order.StatusHistories.Add(CreateHistory(order.Id, oldStatus, newStatus, userId, role, note));
    }
}
