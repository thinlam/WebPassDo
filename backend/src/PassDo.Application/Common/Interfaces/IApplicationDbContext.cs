using Microsoft.EntityFrameworkCore;
using PassDo.Domain.Entities;

namespace PassDo.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<Favorite> Favorites { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderPayment> OrderPayments { get; }
    DbSet<OrderShipment> OrderShipments { get; }
    DbSet<OrderStatusHistory> OrderStatusHistories { get; }
    DbSet<UserAddress> UserAddresses { get; }
    DbSet<UserBankAccount> UserBankAccounts { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
