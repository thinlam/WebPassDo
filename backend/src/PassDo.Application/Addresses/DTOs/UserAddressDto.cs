namespace PassDo.Application.Addresses.DTOs;

public class UserAddressDto
{
    public Guid Id { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string AddressType { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string FullAddress { get; set; } = string.Empty;
}
