using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Options;
using PassDo.Application.Orders.Commands.OrderActions;
using PassDo.Application.Orders.Queries.GetOrderById;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;
using PassDo.Infrastructure.Persistence;

namespace PassDo.UnitTests.Orders;

public class OrderOwnershipTests
{
    private static (PassDoDbContext Db, Mock<ICurrentUserService> CurrentUser, OrderActionHandler Handler) CreateSut(Guid actorId)
    {
        var options = new DbContextOptionsBuilder<PassDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(actorId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Role).Returns("User");

        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var db = new PassDoDbContext(options, currentUser.Object, dateTime.Object);
        var shipping = new Mock<IShippingCalculator>();
        var notifications = new Mock<INotificationService>();

        var handler = new OrderActionHandler(
            db,
            currentUser.Object,
            shipping.Object,
            dateTime.Object,
            notifications.Object);

        return (db, currentUser, handler);
    }

    private static Order SeedOrder(
        PassDoDbContext db,
        Guid buyerId,
        Guid sellerId,
        OrderStatus status = OrderStatus.PendingConfirmation)
    {
        var productId = Guid.NewGuid();
        db.Users.AddRange(
            new User
            {
                Id = buyerId,
                Email = "buyer@test.com",
                FullName = "Buyer",
                PasswordHash = "x",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = sellerId,
                Email = "seller@test.com",
                FullName = "Seller",
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
            Condition = ProductCondition.Used,
            Status = ProductStatus.Reserved,
            Location = "HCM",
            Quantity = 0,
            CategoryId = Guid.NewGuid(),
            SellerId = sellerId,
            CreatedAt = DateTime.UtcNow
        });

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = "PD-TEST-001",
            ProductId = productId,
            BuyerId = buyerId,
            SellerId = sellerId,
            ProductTotal = 100,
            ShippingFee = 0,
            GrandTotal = 100,
            Price = 100,
            Status = status,
            PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Unpaid,
            DeliverySpeed = DeliverySpeed.Standard,
            ShippingRecipientName = "Buyer",
            ShippingPhone = "0900000000",
            ShippingProvince = "HCM",
            ShippingDistrict = "Q1",
            ShippingWard = "P1",
            ShippingStreetAddress = "1 Nguyen Hue",
            PickupRecipientName = "Seller",
            PickupPhone = "0900000001",
            PickupProvince = "HCM",
            PickupDistrict = "Q3",
            PickupWard = "P2",
            PickupStreetAddress = "2 Le Loi",
            CreatedAt = DateTime.UtcNow,
            Items =
            {
                new OrderItem
                {
                    ProductId = productId,
                    ProductName = "Item",
                    UnitPrice = 100,
                    Quantity = 1,
                    LineTotal = 100
                }
            }
        };

        db.Orders.Add(order);
        db.SaveChanges();
        return order;
    }

    [Fact]
    public async Task ConfirmOrder_ThrowsForbidden_WhenActorIsNeitherSellerNorAdmin()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var (db, _, handler) = CreateSut(strangerId);
        var order = SeedOrder(db, buyerId, sellerId, OrderStatus.PendingConfirmation);

        var act = async () => await handler.Handle(new ConfirmOrderCommand(order.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*seller or admin*");
    }

    [Fact]
    public async Task ConfirmOrder_ThrowsForbidden_WhenBuyerTriesToConfirm()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var (db, _, handler) = CreateSut(buyerId);
        var order = SeedOrder(db, buyerId, sellerId, OrderStatus.PendingConfirmation);

        var act = async () => await handler.Handle(new ConfirmOrderCommand(order.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*seller or admin*");
    }

    [Fact]
    public async Task CancelOrder_ThrowsForbidden_WhenSellerTriesToCancelAsBuyer()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var (db, _, handler) = CreateSut(sellerId);
        var order = SeedOrder(db, buyerId, sellerId, OrderStatus.PendingConfirmation);

        var act = async () => await handler.Handle(new CancelOrderCommand(order.Id, "nope"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*buyer*");
    }

    [Fact]
    public async Task GetOrderById_ThrowsForbidden_WhenUserIsNotParticipant()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var (db, currentUser, _) = CreateSut(strangerId);
        var order = SeedOrder(db, buyerId, sellerId);

        var queryHandler = new GetOrderByIdQueryHandler(db, currentUser.Object);
        var act = async () => await queryHandler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*not allowed*");
    }
}
