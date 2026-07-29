using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PassDo.Application.Categories.Commands.CreateCategory;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Products;
using PassDo.Application.Products.Commands.UpdateProductStatus;
using PassDo.Application.Products.Commands.CreateProduct;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;
using PassDo.Infrastructure.Persistence;
using PassDo.Infrastructure.Services;

namespace PassDo.UnitTests.Products;

public class ProductStatusTests
{
    private static PassDoDbContext CreateDb(Guid? userId = null, string role = "User")
    {
        var options = new DbContextOptionsBuilder<PassDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(userId.HasValue);
        currentUser.Setup(x => x.Role).Returns(role);
        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        return new PassDoDbContext(options, currentUser.Object, dateTime.Object);
    }

    [Theory]
    [InlineData(ProductStatus.Draft, ProductStatus.PendingReview, true)]
    [InlineData(ProductStatus.PendingReview, ProductStatus.Draft, true)]
    [InlineData(ProductStatus.Rejected, ProductStatus.Draft, true)]
    [InlineData(ProductStatus.Active, ProductStatus.Hidden, true)]
    [InlineData(ProductStatus.Hidden, ProductStatus.Active, true)]
    [InlineData(ProductStatus.Draft, ProductStatus.Active, false)]
    [InlineData(ProductStatus.PendingReview, ProductStatus.Active, false)]
    [InlineData(ProductStatus.Rejected, ProductStatus.PendingReview, false)]
    public void Seller_transitions_matrix(ProductStatus from, ProductStatus to, bool expected)
    {
        ProductStatusTransitions.CanSellerTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData(ProductStatus.PendingReview, ProductStatus.Active, true)]
    [InlineData(ProductStatus.PendingReview, ProductStatus.Rejected, true)]
    [InlineData(ProductStatus.Draft, ProductStatus.Active, false)]
    public void Admin_transitions_matrix(ProductStatus from, ProductStatus to, bool expected)
    {
        ProductStatusTransitions.CanAdminTransition(from, to).Should().Be(expected);
    }

    [Fact]
    public async Task CreateProduct_AlwaysDraft_IgnoresClientActive()
    {
        var sellerId = Guid.NewGuid();
        await using var db = CreateDb(sellerId);
        var category = await new CreateCategoryCommandHandler(db).Handle(
            new CreateCategoryCommand("Cat", null, "cat", 1, true), CancellationToken.None);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(sellerId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Role).Returns("User");

        var result = await new CreateProductCommandHandler(db, currentUser.Object).Handle(
            new CreateProductCommand(
                "Item", "Desc", 100, 50, ProductCondition.LikeNew, category.Id,
                "HCM", 1, null, null, AcceptedPaymentOption.CashOnDelivery,
                new[] { DeliverySpeed.Standard }, ProductStatus.Active),
            CancellationToken.None);

        result.Status.Should().Be(nameof(ProductStatus.Draft));
    }

    [Fact]
    public async Task UpdateProductStatus_SellerCannotPendingReviewToActive()
    {
        var sellerId = Guid.NewGuid();
        await using var db = CreateDb(sellerId, "User");

        var category = new Category { Name = "Cat", Slug = "cat", DisplayOrder = 1, IsActive = true };
        var product = new Product
        {
            Name = "Item",
            Description = "Desc",
            OriginalPrice = 100,
            SellingPrice = 50,
            Condition = ProductCondition.LikeNew,
            Status = ProductStatus.PendingReview,
            Location = "HCM",
            Quantity = 1,
            CategoryId = category.Id,
            SellerId = sellerId
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(sellerId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Role).Returns("User");

        var handler = new UpdateProductStatusCommandHandler(db, currentUser.Object);

        Func<Task> act = () => handler.Handle(
            new UpdateProductStatusCommand(product.Id, ProductStatus.Active),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<Exception>();
        (ex.Which is ForbiddenException or ConflictException).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProductStatus_AdminCanPendingReviewToActive()
    {
        var sellerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        await using var db = CreateDb(adminId, "Admin");

        var category = new Category { Name = "Cat", Slug = "cat", DisplayOrder = 1, IsActive = true };
        var product = new Product
        {
            Name = "Item",
            Description = "Desc",
            OriginalPrice = 100,
            SellingPrice = 50,
            Condition = ProductCondition.LikeNew,
            Status = ProductStatus.PendingReview,
            Location = "HCM",
            Quantity = 1,
            CategoryId = category.Id,
            SellerId = sellerId
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(adminId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Role).Returns("Admin");

        var handler = new UpdateProductStatusCommandHandler(db, currentUser.Object);

        var dto = await handler.Handle(
            new UpdateProductStatusCommand(product.Id, ProductStatus.Active),
            CancellationToken.None);

        dto.Status.Should().Be(nameof(ProductStatus.Active));
    }

    [Fact]
    public async Task UpdateProductStatus_SellerCanDraftToPendingReview()
    {
        var sellerId = Guid.NewGuid();
        await using var db = CreateDb(sellerId, "User");

        var category = new Category { Name = "Cat", Slug = "cat", DisplayOrder = 1, IsActive = true };
        var product = new Product
        {
            Name = "Item",
            Description = "Desc",
            OriginalPrice = 100,
            SellingPrice = 50,
            Condition = ProductCondition.LikeNew,
            Status = ProductStatus.Draft,
            Location = "HCM",
            Quantity = 1,
            CategoryId = category.Id,
            SellerId = sellerId
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(sellerId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Role).Returns("User");

        var handler = new UpdateProductStatusCommandHandler(db, currentUser.Object);

        var dto = await handler.Handle(
            new UpdateProductStatusCommand(product.Id, ProductStatus.PendingReview),
            CancellationToken.None);

        dto.Status.Should().Be(nameof(ProductStatus.PendingReview));
    }
}
