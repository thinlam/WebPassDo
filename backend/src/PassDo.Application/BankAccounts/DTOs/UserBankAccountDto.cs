namespace PassDo.Application.BankAccounts.DTOs;

public class UserBankAccountDto
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountNumberMasked { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public bool IsDefault { get; set; }
}
