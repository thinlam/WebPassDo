using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Moq;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Options;
using PassDo.Application.Orders.Commands.OrderActions;
using PassDo.Domain.Constants;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;
using PassDo.Infrastructure.Persistence;

namespace PassDo.UnitTests.Orders;

public class RejectOrderCommandTests
{
    private static (PassDoDbContext Db, Mock<ICurrentUserService> CurrentUser, Mock<INotificationService> Notifications, OrderActionHandler Handler)
        CreateSut(Guid actorId)
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

        return (db, currentUser, notifications, handler);
    }

    private static Order SeedOrder(
        PassDoDbContext db,
        Guid buyerId,
        Guid sellerId,
        OrderStatus status = OrderStatus.PendingSellerConfirmation)
    {
        var productId = Guid.NewGuid();
        db.Users.AddRange(
            new User { Id = buyerId, Email = "buyer@test.com", FullName = "Buyer", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = sellerId, Email = "seller@test.com", FullName = "Seller", PasswordHash = "x", CreatedAt = DateTime.UtcNow });

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
            OrderCode = "PD-TEST-002",
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
                new OrderItem { ProductId = productId, ProductName = "Item", UnitPrice = 100, Quantity = 1, LineTotal = 100 }
            }
        };

        db.Orders.Add(order);
        db.SaveChanges();
        return order;
    }

    [Theory]
    [InlineData(OrderRejectReason.OutOfStock, "Hết hàng")]
    [InlineData(OrderRejectReason.SoldElsewhere, "Đã bán nơi khác")]
    [InlineData(OrderRejectReason.CannotDeliver, "Không giao được")]
    [InlineData(OrderRejectReason.WrongPrice, "Sai giá")]
    public async Task RejectOrder_NonOtherReason_SetsCancellationReasonToLabel(OrderRejectReason code, string expectedLabel)
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var (db, _, notifications, handler) = CreateSut(sellerId);
        var order = SeedOrder(db, buyerId, sellerId);

        var result = await handler.Handle(new RejectOrderCommand(order.Id, code, null), CancellationToken.None);

        result.CancellationReason.Should().Be(expectedLabel);
        result.Status.Should().Be("Cancelled");
        notifications.Verify(x => x.NotifyAsync(
            buyerId,
            NotificationTypes.OrderRejected,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectOrder_OtherReasonWithNote_FormatsAsKhacPrefix()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var (db, _, _, handler) = CreateSut(sellerId);
        var order = SeedOrder(db, buyerId, sellerId);

        var result = await handler.Handle(
            new RejectOrderCommand(order.Id, OrderRejectReason.Other, "Đổi ý không bán nữa"),
            CancellationToken.None);

        result.CancellationReason.Should().Be("Khác: Đổi ý không bán nữa");
    }

    [Fact]
    public void Validator_OtherReasonWithoutNote_Fails()
    {
        var validator = new RejectOrderCommandValidator();
        var result = validator.TestValidate(new RejectOrderCommand(Guid.NewGuid(), OrderRejectReason.Other, null));

        result.ShouldHaveValidationErrorFor(x => x.ReasonNote);
    }

    [Fact]
    public void Validator_OtherReasonWithWhitespaceNote_Fails()
    {
        var validator = new RejectOrderCommandValidator();
        var result = validator.TestValidate(new RejectOrderCommand(Guid.NewGuid(), OrderRejectReason.Other, "   "));

        result.ShouldHaveValidationErrorFor(x => x.ReasonNote);
    }

    [Fact]
    public void Validator_NonOtherReasonWithoutNote_Passes()
    {
        var validator = new RejectOrderCommandValidator();
        var result = validator.TestValidate(new RejectOrderCommand(Guid.NewGuid(), OrderRejectReason.OutOfStock, null));

        result.ShouldNotHaveValidationErrorFor(x => x.ReasonNote);
    }

    [Fact]
    public void Validator_NoteExceeding500Chars_Fails()
    {
        var validator = new RejectOrderCommandValidator();
        var longNote = new string('a', 501);
        var result = validator.TestValidate(new RejectOrderCommand(Guid.NewGuid(), OrderRejectReason.OutOfStock, longNote));

        result.ShouldHaveValidationErrorFor(x => x.ReasonNote);
    }

    [Fact]
    public async Task CancelOrder_ByBuyer_StillUsesOrderCancelledNotification()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var (db, _, notifications, handler) = CreateSut(buyerId);
        var order = SeedOrder(db, buyerId, sellerId, OrderStatus.AwaitingPayment);

        await handler.Handle(new CancelOrderCommand(order.Id, "Đổi ý"), CancellationToken.None);

        notifications.Verify(x => x.NotifyAsync(
            sellerId,
            NotificationTypes.OrderCancelled,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

