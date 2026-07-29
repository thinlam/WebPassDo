using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Options;
using PassDo.Application.Orders.Commands.CreateOrder;
using PassDo.Application.Orders.Commands.OrderActions;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;
using PassDo.Infrastructure.Persistence;
using PassDo.Infrastructure.Services;

namespace PassDo.UnitTests.Orders;

public class ProductOrderReservationTests
{
    private static (PassDoDbContext Db, Mock<ICurrentUserService> CurrentUser, Mock<IDateTimeProvider> Clock) CreateDb(
        string dbName,
        Guid userId)
    {
        var options = new DbContextOptionsBuilder<PassDoDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Role).Returns("User");

        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        return (new PassDoDbContext(options, currentUser.Object, clock.Object), currentUser, clock);
    }

    private static async Task SeedUsersAddressesAndProduct(
        PassDoDbContext db,
        Guid sellerId,
        Guid buyerId,
        Guid? secondBuyerId,
        Guid productId,
        ProductStatus productStatus,
        int productQty,
        Guid buyerShippingAddressId,
        Guid? secondBuyerShippingAddressId)
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

        if (secondBuyerId.HasValue)
        {
            db.Users.Add(new User
            {
                Id = secondBuyerId.Value,
                Email = "buyer2@test.com",
                FullName = "Buyer2",
                PasswordHash = "x",
                CreatedAt = DateTime.UtcNow
            });
        }

        db.UserAddresses.Add(new UserAddress
        {
            UserId = sellerId,
            RecipientName = "Seller",
            PhoneNumber = "0900000000",
            Province = "HCM",
            District = "District 1",
            Ward = "Ward 1",
            StreetAddress = "1 Seller St",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });

        db.UserAddresses.Add(new UserAddress
        {
            Id = buyerShippingAddressId,
            UserId = buyerId,
            RecipientName = "Buyer",
            PhoneNumber = "0900000001",
            Province = "HCM",
            District = "District 1",
            Ward = "Ward 2",
            StreetAddress = "2 Buyer St",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });

        if (secondBuyerId.HasValue && secondBuyerShippingAddressId.HasValue)
        {
            db.UserAddresses.Add(new UserAddress
            {
                Id = secondBuyerShippingAddressId.Value,
                UserId = secondBuyerId.Value,
                RecipientName = "Buyer2",
                PhoneNumber = "0900000002",
                Province = "HCM",
                District = "District 1",
                Ward = "Ward 3",
                StreetAddress = "3 Buyer2 St",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        db.Products.Add(new Product
        {
            Id = productId,
            Name = "Item",
            Description = "desc",
            SellingPrice = 100,
            OriginalPrice = 200,
            Condition = ProductCondition.New,
            Status = productStatus,
            Location = "HCM",
            Quantity = productQty,
            CategoryId = Guid.NewGuid(),
            SellerId = sellerId,
            AcceptedPaymentOption = AcceptedPaymentOption.CashOnDelivery,
            AllowedDeliverySpeeds = "Standard,Intercity",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateOrder_OnActive_SetsProductReserved()
    {
        var dbName = Guid.NewGuid().ToString();
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var buyerAddressId = Guid.NewGuid();

        var (db, currentUser, clock) = CreateDb(dbName, buyerId);
        await SeedUsersAddressesAndProduct(
            db,
            sellerId,
            buyerId,
            secondBuyerId: null,
            productId,
            ProductStatus.Active,
            productQty: 10,
            buyerAddressId,
            secondBuyerShippingAddressId: null);

        var shipping = new ShippingCalculator(Options.Create(new ShippingOptions()));
        var notifications = new Mock<INotificationService>();

        var handler = new CreateOrderCommandHandler(db, currentUser.Object, shipping, clock.Object, notifications.Object);
        await handler.Handle(
            new CreateOrderCommand(
                productId,
                1,
                buyerAddressId,
                DeliverySpeed.Standard,
                PaymentMethod.CashOnDelivery,
                null),
            CancellationToken.None);

        var reloaded = await db.Products.AsNoTracking().FirstAsync(x => x.Id == productId);
        reloaded.Status.Should().Be(ProductStatus.Reserved);
    }

    [Fact]
    public async Task CreateOrder_Throws_WhenProductNotActive()
    {
        var dbName = Guid.NewGuid().ToString();
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var buyerAddressId = Guid.NewGuid();

        var (db, currentUser, clock) = CreateDb(dbName, buyerId);
        await SeedUsersAddressesAndProduct(
            db,
            sellerId,
            buyerId,
            secondBuyerId: null,
            productId,
            ProductStatus.Reserved,
            productQty: 10,
            buyerAddressId,
            secondBuyerShippingAddressId: null);

        var shipping = new ShippingCalculator(Options.Create(new ShippingOptions()));
        var notifications = new Mock<INotificationService>();
        var handler = new CreateOrderCommandHandler(db, currentUser.Object, shipping, clock.Object, notifications.Object);

        var act = async () => await handler.Handle(
            new CreateOrderCommand(
                productId,
                1,
                buyerAddressId,
                DeliverySpeed.Standard,
                PaymentMethod.CashOnDelivery,
                null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*not available*");
    }

    [Fact]
    public async Task CreateOrder_Throws_WhenAnotherNonTerminalOrderExists_ForSameProduct()
    {
        var dbName = Guid.NewGuid().ToString();
        var sellerId = Guid.NewGuid();
        var buyer1Id = Guid.NewGuid();
        var buyer2Id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var buyer1AddressId = Guid.NewGuid();
        var buyer2AddressId = Guid.NewGuid();

        var (db1, currentUser1, clock1) = CreateDb(dbName, buyer1Id);
        await SeedUsersAddressesAndProduct(
            db1,
            sellerId,
            buyer1Id,
            secondBuyerId: buyer2Id,
            productId,
            ProductStatus.Active,
            productQty: 10,
            buyer1AddressId,
            buyer2AddressId);

        var shipping = new ShippingCalculator(Options.Create(new ShippingOptions()));
        var notifications = new Mock<INotificationService>();

        var handler1 = new CreateOrderCommandHandler(db1, currentUser1.Object, shipping, clock1.Object, notifications.Object);
        await handler1.Handle(
            new CreateOrderCommand(
                productId,
                1,
                buyer1AddressId,
                DeliverySpeed.Standard,
                PaymentMethod.CashOnDelivery,
                null),
            CancellationToken.None);

        var (db2, currentUser2, clock2) = CreateDb(dbName, buyer2Id);
        var handler2 = new CreateOrderCommandHandler(db2, currentUser2.Object, shipping, clock2.Object, notifications.Object);

        var act = async () => await handler2.Handle(
            new CreateOrderCommand(
                productId,
                1,
                buyer2AddressId,
                DeliverySpeed.Standard,
                PaymentMethod.CashOnDelivery,
                null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CancelOrder_RestoresProductToActive()
    {
        var dbName = Guid.NewGuid().ToString();
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var buyerAddressId = Guid.NewGuid();

        var (db, currentUser, clock) = CreateDb(dbName, buyerId);
        await SeedUsersAddressesAndProduct(
            db,
            sellerId,
            buyerId,
            secondBuyerId: null,
            productId,
            ProductStatus.Active,
            productQty: 2,
            buyerAddressId,
            secondBuyerShippingAddressId: null);

        var shipping = new ShippingCalculator(Options.Create(new ShippingOptions()));
        var notifications = new Mock<INotificationService>();

        var createHandler = new CreateOrderCommandHandler(db, currentUser.Object, shipping, clock.Object, notifications.Object);
        var created = await createHandler.Handle(
            new CreateOrderCommand(
                productId,
                1,
                buyerAddressId,
                DeliverySpeed.Standard,
                PaymentMethod.CashOnDelivery,
                null),
            CancellationToken.None);

        var actionHandler = new OrderActionHandler(db, currentUser.Object, shipping, clock.Object, notifications.Object);
        await actionHandler.Handle(new CancelOrderCommand(created.Id, "changed mind"), CancellationToken.None);

        var reloaded = await db.Products.AsNoTracking().FirstAsync(x => x.Id == productId);
        reloaded.Status.Should().Be(ProductStatus.Active);
    }
}

