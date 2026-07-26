using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Options;
using PassDo.Application.Favorites.Commands.AddFavorite;
using PassDo.Application.Orders.Commands.CreateOrder;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;
using PassDo.Infrastructure.Persistence;
using PassDo.Infrastructure.Services;

namespace PassDo.UnitTests.Orders;

public class FavoriteAndOrderHandlerTests
{
    private static (PassDoDbContext Db, Mock<ICurrentUserService> CurrentUser) CreateDb(Guid userId)
    {
        var options = new DbContextOptionsBuilder<PassDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Role).Returns("User");

        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        return (new PassDoDbContext(options, currentUser.Object, dateTime.Object), currentUser);
    }

    [Fact]
    public async Task AddFavorite_Throws_WhenFavoritingOwnProduct()
    {
        var sellerId = Guid.NewGuid();
        var (db, currentUser) = CreateDb(sellerId);

        var product = new Product
        {
            Name = "Own product",
            Description = "desc",
            SellingPrice = 100,
            OriginalPrice = 200,
            Condition = ProductCondition.Used,
            Status = ProductStatus.Available,
            Location = "HCM",
            Quantity = 1,
            CategoryId = Guid.NewGuid(),
            SellerId = sellerId,
            CreatedAt = DateTime.UtcNow
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new AddFavoriteCommandHandler(db, currentUser.Object);
        var act = async () => await handler.Handle(new AddFavoriteCommand(product.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*own product*");
    }

    [Fact]
    public async Task CreateOrder_Throws_WhenBuyingOwnProduct()
    {
        var sellerId = Guid.NewGuid();
        var (db, currentUser) = CreateDb(sellerId);

        db.Users.Add(new User
        {
            Id = sellerId,
            Email = "seller@test.com",
            FullName = "Seller",
            PasswordHash = "x",
            CreatedAt = DateTime.UtcNow
        });

        var product = new Product
        {
            Name = "Item",
            Description = "desc",
            SellingPrice = 100,
            OriginalPrice = 200,
            Condition = ProductCondition.New,
            Status = ProductStatus.Available,
            Location = "HCM",
            Quantity = 1,
            CategoryId = Guid.NewGuid(),
            SellerId = sellerId,
            CreatedAt = DateTime.UtcNow
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var shipping = new ShippingCalculator(Options.Create(new ShippingOptions()));
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        var notifications = new Mock<INotificationService>();

        var handler = new CreateOrderCommandHandler(db, currentUser.Object, shipping, clock.Object, notifications.Object);
        var act = async () => await handler.Handle(
            new CreateOrderCommand(
                product.Id,
                1,
                Guid.NewGuid(),
                DeliverySpeed.Standard,
                PaymentMethod.CashOnDelivery,
                null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*own product*");
    }
}
