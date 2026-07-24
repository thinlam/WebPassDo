using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;
using PassDo.Application.Common.Interfaces;

namespace PassDo.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var dbContext = scope.ServiceProvider.GetRequiredService<PassDoDbContext>();

        var shouldApplyMigrations =
            environment.IsDevelopment()
            || string.Equals(
                Environment.GetEnvironmentVariable("APPLY_MIGRATIONS"),
                "true",
                StringComparison.OrdinalIgnoreCase);

        if (shouldApplyMigrations)
        {
            logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await SeedAsync(dbContext, environment, logger, passwordHasher, cancellationToken);
    }

    private static async Task SeedAsync(
        PassDoDbContext dbContext,
        IHostEnvironment environment,
        ILogger logger,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Role == UserRole.Admin, cancellationToken))
        {
            var admin = new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "admin@passdo.local",
                FullName = "PassDo Admin",
                PasswordHash = passwordHasher.Hash("Admin@123456"),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(admin);
            logger.LogInformation("Seeded admin user admin@passdo.local");
        }

        if (!await dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Role == UserRole.Shipper, cancellationToken))
        {
            var shipper = new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "shipper@passdo.local",
                FullName = "PassDo Shipper",
                PhoneNumber = "0900000002",
                PasswordHash = passwordHasher.Hash("Shipper@123456"),
                Role = UserRole.Shipper,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(shipper);
            logger.LogInformation("Seeded shipper user shipper@passdo.local");
        }

        if (!await dbContext.Categories.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var categories = new[]
            {
                new Category { Name = "Mỹ phẩm", Description = "Skincare, makeup", Slug = "my-pham", DisplayOrder = 1, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Thời trang", Description = "Quần áo, phụ kiện", Slug = "thoi-trang", DisplayOrder = 2, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Điện tử", Description = "Thiết bị điện tử", Slug = "dien-tu", DisplayOrder = 3, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Đồ gia dụng", Description = "Đồ dùng nhà cửa", Slug = "do-gia-dung", DisplayOrder = 4, CreatedAt = DateTime.UtcNow }
            };

            dbContext.Categories.AddRange(categories);
            logger.LogInformation("Seeded default categories");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (environment.IsDevelopment()
            && !await dbContext.Products.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var admin = await dbContext.Users.FirstAsync(u => u.Role == UserRole.Admin, cancellationToken);
            var category = await dbContext.Categories.FirstAsync(cancellationToken);

            var sampleProducts = new[]
            {
                new Product
                {
                    Name = "Kem chống nắng Anessa",
                    Description = "Đã sử dụng khoảng 20%, còn hạn đến tháng 10/2027",
                    OriginalPrice = 650000,
                    SellingPrice = 350000,
                    Condition = ProductCondition.Used,
                    Status = ProductStatus.Available,
                    Quantity = 1,
                    CategoryId = category.Id,
                    SellerId = admin.Id,
                    Location = "Bình Thạnh, TP.HCM",
                    AllowedDeliverySpeeds = "Express,SameDay,Standard,Intercity",
                    AcceptedPaymentOption = AcceptedPaymentOption.Both,
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Áo khoác Uniqlo",
                    Description = "Size M, mặc vài lần, còn mới 95%",
                    OriginalPrice = 899000,
                    SellingPrice = 450000,
                    Condition = ProductCondition.LikeNew,
                    Status = ProductStatus.Available,
                    Quantity = 1,
                    CategoryId = category.Id,
                    SellerId = admin.Id,
                    Location = "Quận 1, TP.HCM",
                    AllowedDeliverySpeeds = "Standard,Intercity",
                    AcceptedPaymentOption = AcceptedPaymentOption.Both,
                    CreatedAt = DateTime.UtcNow
                }
            };

            dbContext.Products.AddRange(sampleProducts);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded sample products for Development");
        }
    }
}
