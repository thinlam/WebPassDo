using PassDo.Domain.Common;
using PassDo.Domain.Enums;

namespace PassDo.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime? DateOfBirth { get; set; }
    public DateTime? LastSeenAt { get; set; }

    /// <summary>Google account subject ("sub" claim) when the user has linked/registered via Google Sign-In.</summary>
    public string? GoogleSubject { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Order> Purchases { get; set; } = new List<Order>();
    public ICollection<Order> Sales { get; set; } = new List<Order>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public ICollection<UserBankAccount> BankAccounts { get; set; } = new List<UserBankAccount>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
