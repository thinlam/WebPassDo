using PassDo.Domain.Common;

namespace PassDo.Domain.Entities;

public class UserBankAccount : BaseEntity
{
    public Guid UserId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public bool IsDefault { get; set; }

    public User User { get; set; } = null!;
}
