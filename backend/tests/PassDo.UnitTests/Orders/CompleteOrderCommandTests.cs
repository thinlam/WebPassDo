using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Options;
using PassDo.Application.Orders.Commands.OrderActions;
using PassDo.Domain.Constants;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;
using PassDo.Infrastructure.Persistence;
using PassDo.Infrastructure.Services;

namespace PassDo.UnitTests.Orders;

public class CompleteOrderCommandTests
{
    private static (PassDoDbContext Db, Mock<ICurrentUserService> CurrentUser, Mock<IDateTimeProvider> Clock) CreateDb(
        string dbName,
        Guid userId,
        string role = "User")
    {
        var options = new DbContextOptionsBuilder<PassDoDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Role).Returns(role);

        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        return (new PassDoDbContext(options, currentUser.Object, clock.Object), currentUser, clock);
    }

    private static async Task SeedUsersProductAndOrder(
        PassDoDbContext db,
        Guid orderId,
        Guid productId,
        Guid sellerId,
        Guid buyerId,
        OrderStatus status,
        DateTime? deliveredAt = null,
        DateTime? completedAt = null)
    {
        db.Users.Add(new User
        {
            Id = sellerId,
            Email = "seller@test.com",
            FullName = "Seller",
            PasswordHash = "x",
            CreatedAt = DateTime.UtcNow
        });

        db.Users.Add(new User
        {
            Id = buyerId,
            Email = "buyer@test.com",
            FullName = "Buyer",
            PasswordHash = "x",
            CreatedAt = DateTime.UtcNow
        });

        db.Products.Add(new Product
        {
            Id = productId,
            Name = "Item",
            Description = "desc",
            SellingPrice = 100,
            OriginalPrice = 200,
            Condition = ProductCondition.New,
            Status = ProductStatus.Reserved,
            Location = "HCM",
            Quantity = 1,
            CategoryId = Guid.NewGuid(),
            SellerId = sellerId,
            AcceptedPaymentOption = AcceptedPaymentOption.CashOnDelivery,
            AllowedDeliverySpeeds = "Standard,Intercity",
            CreatedAt = DateTime.UtcNow
        });

        var order = new Order
        {
            Id = orderId,
            OrderCode = "ORD-TEST",
            ProductId = productId,
            BuyerId = buyerId,
            SellerId = sellerId,
            ProductTotal = 100,
            ShippingFee = 10,
            GrandTotal = 110,
            Status = status,
            PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Paid,
            DeliverySpeed = DeliverySpeed.Standard,
            DeliveredAt = deliveredAt,
            CompletedAt = completedAt,

            ShippingRecipientName = "Buyer",
            ShippingPhone = "0900000001",
            ShippingProvince = "HCM",
            ShippingDistrict = "District 1",
            ShippingWard = "Ward 1",
            ShippingStreetAddress = "1 Buyer St",

            PickupRecipientName = "Seller",
            PickupPhone = "0900000000",
            PickupProvince = "HCM",
            PickupDistrict = "District 1",
            PickupWard = "Ward 1",
            PickupStreetAddress = "1 Seller St",

            Price = 110
        };

        db.Orders.Add(order);
        db.OrderItems.Add(new OrderItem
        {
            OrderId = orderId,
            ProductId = productId,
            ProductName = "Item",
            ProductImageUrl = null,
            UnitPrice = 100,
            Quantity = 1,
            LineTotal = 100
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CompleteOrder_BuyerCanConfirm_WhenDelivered()
    {
        var dbName = Guid.NewGuid().ToString();
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var (db, currentUser, clock) = CreateDb(dbName, buyerId);
        var now = new DateTime(2026, 7, 29, 0, 0, 0, DateTimeKind.Utc);
        clock.Setup(x => x.UtcNow).Returns(now);

        await SeedUsersProductAndOrder(
            db,
            orderId,
            productId,
            sellerId,
            buyerId,
            OrderStatus.Delivered,
            deliveredAt: now.AddDays(-1));

        var shipping = new ShippingCalculator(Options.Create(new ShippingOptions()));
        var notifications = new Mock<INotificationService>();
        var handler = new OrderActionHandler(db, currentUser.Object, shipping, clock.Object, notifications.Object);

        await handler.Handle(new CompleteOrderCommand(orderId), CancellationToken.None);

        var reloaded = await db.Orders.AsNoTracking().FirstAsync(x => x.Id == orderId);
        reloaded.Status.Should().Be(OrderStatus.Completed);
        reloaded.CompletedAt.Should().Be(now);

        var history = await db.OrderStatusHistories.AsNoTracking().OrderByDescending(x => x.CreatedAt).FirstAsync(x => x.OrderId == orderId);
        history.OldStatus.Should().Be(OrderStatus.Delivered);
        history.NewStatus.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public async Task CompleteOrder_AdminCanConfirm_WhenDelivered()
    {
        var dbName = Guid.NewGuid().ToString();
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var (db, currentUser, clock) = CreateDb(dbName, adminId, role: Roles.Admin);
        var now = new DateTime(2026, 7, 29, 0, 0, 0, DateTimeKind.Utc);
        clock.Setup(x => x.UtcNow).Returns(now);

        await SeedUsersProductAndOrder(
            db,
            orderId,
            productId,
            sellerId,
            buyerId,
            OrderStatus.Delivered,
            deliveredAt: now.AddHours(-3));

        var shipping = new ShippingCalculator(Options.Create(new ShippingOptions()));
        var notifications = new Mock<INotificationService>();
        var handler = new OrderActionHandler(db, currentUser.Object, shipping, clock.Object, notifications.Object);

        await handler.Handle(new CompleteOrderCommand(orderId), CancellationToken.None);

        var reloaded = await db.Orders.AsNoTracking().FirstAsync(x => x.Id == orderId);
        reloaded.Status.Should().Be(OrderStatus.Completed);
        reloaded.CompletedAt.Should().Be(now);
    }

    [Fact]
    public async Task CompleteOrder_Idempotent_WhenAlreadyCompleted()
    {
        var dbName = Guid.NewGuid().ToString();
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var (db, currentUser, clock) = CreateDb(dbName, buyerId);
        var completedAt = new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc);
        clock.Setup(x => x.UtcNow).Returns(new DateTime(2026, 7, 29, 0, 0, 0, DateTimeKind.Utc));

        await SeedUsersProductAndOrder(
            db,
            orderId,
            productId,
            sellerId,
            buyerId,
            OrderStatus.Completed,
            deliveredAt: completedAt.AddHours(-1),
            completedAt: completedAt);

        var shipping = new ShippingCalculator(Options.Create(new ShippingOptions()));
        var notifications = new Mock<INotificationService>();
        var handler = new OrderActionHandler(db, currentUser.Object, shipping, clock.Object, notifications.Object);

        await handler.Handle(new CompleteOrderCommand(orderId), CancellationToken.None);

        var reloaded = await db.Orders.AsNoTracking().FirstAsync(x => x.Id == orderId);
        reloaded.Status.Should().Be(OrderStatus.Completed);
        reloaded.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public async Task CompleteOrder_Rejects_SellerCall()
    {
        var dbName = Guid.NewGuid().ToString();
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var (db, currentUser, clock) = CreateDb(dbName, sellerId);
        await SeedUsersProductAndOrder(db, orderId, productId, sellerId, buyerId, OrderStatus.Delivered);

        var shipping = new ShippingCalculator(Options.Create(new ShippingOptions()));
        var notifications = new Mock<INotificationService>();
        var handler = new OrderActionHandler(db, currentUser.Object, shipping, clock.Object, notifications.Object);

        var act = async () => await handler.Handle(new CompleteOrderCommand(orderId), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CompleteOrder_Rejects_NonDelivered()
    {
        var dbName = Guid.NewGuid().ToString();
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var (db, currentUser, clock) = CreateDb(dbName, buyerId);
        await SeedUsersProductAndOrder(db, orderId, productId, sellerId, buyerId, OrderStatus.Shipping);

        var shipping = new ShippingCalculator(Options.Create(new ShippingOptions()));
        var notifications = new Mock<INotificationService>();
        var handler = new OrderActionHandler(db, currentUser.Object, shipping, clock.Object, notifications.Object);

        var act = async () => await handler.Handle(new CompleteOrderCommand(orderId), CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }
}

