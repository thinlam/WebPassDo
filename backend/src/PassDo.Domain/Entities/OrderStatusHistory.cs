using PassDo.Domain.Common;
using PassDo.Domain.Enums;

namespace PassDo.Domain.Entities;

public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public OrderStatus? OldStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? ChangedByRole { get; set; }
    public string? Note { get; set; }

    public Order Order { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
