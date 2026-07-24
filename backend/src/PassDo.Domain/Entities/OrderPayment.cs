using PassDo.Domain.Common;
using PassDo.Domain.Enums;

namespace PassDo.Domain.Entities;

public class OrderPayment : BaseEntity
{
    public Guid OrderId { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;
    public decimal Amount { get; set; }
    public string? TransferContent { get; set; }
    public string? ProofImageUrl { get; set; }
    public Guid? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? Note { get; set; }

    public Order Order { get; set; } = null!;
    public User? ConfirmedByUser { get; set; }
}
