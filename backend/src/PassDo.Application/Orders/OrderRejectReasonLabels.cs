using PassDo.Domain.Enums;

namespace PassDo.Application.Orders;

public static class OrderRejectReasonLabels
{
    private static readonly Dictionary<OrderRejectReason, string> Labels = new()
    {
        [OrderRejectReason.OutOfStock] = "Hết hàng",
        [OrderRejectReason.SoldElsewhere] = "Đã bán nơi khác",
        [OrderRejectReason.CannotDeliver] = "Không giao được",
        [OrderRejectReason.WrongPrice] = "Sai giá",
        [OrderRejectReason.Other] = "Khác"
    };

    public static string Get(OrderRejectReason code) => Labels[code];

    public static string Format(OrderRejectReason code, string? note)
    {
        if (code == OrderRejectReason.Other)
        {
            return $"Khác: {note}";
        }

        var label = Get(code);
        return string.IsNullOrWhiteSpace(note) ? label : $"{label} — {note}";
    }
}
