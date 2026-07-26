using PassDo.Domain.Common;
using PassDo.Domain.Enums;

namespace PassDo.Domain.Entities;

public class UserAddress : BaseEntity
{
    public Guid UserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? WardCode { get; set; }
    public string StreetAddress { get; set; } = string.Empty;
    public string? Note { get; set; }
    public AddressType AddressType { get; set; } = AddressType.Home;
    public bool IsDefault { get; set; }

    public User User { get; set; } = null!;
}
