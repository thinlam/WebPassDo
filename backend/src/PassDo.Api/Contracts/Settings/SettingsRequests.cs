using PassDo.Domain.Enums;

namespace PassDo.Api.Contracts.Settings;

public record UpsertAddressRequest(
    string RecipientName,
    string PhoneNumber,
    string Province,
    string District,
    string Ward,
    string StreetAddress,
    string? Note,
    AddressType AddressType,
    bool IsDefault);

public record UpsertBankAccountRequest(
    string BankName,
    string AccountNumber,
    string AccountHolderName,
    string? Branch,
    bool IsDefault);
