using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PassDo.Application.Categories.Commands.CreateCategory;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Products.Commands.CreateProduct;
using PassDo.Domain.Enums;
using PassDo.Infrastructure.Persistence;
using PassDo.Infrastructure.Services;

namespace PassDo.UnitTests.Products;

public class CategoryAndProductHandlerTests
{
    private static PassDoDbContext CreateDbContext(Guid? userId = null)
    {
        var options = new DbContextOptionsBuilder<PassDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(userId.HasValue);
        currentUser.Setup(x => x.Role).Returns("User");

        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        return new PassDoDbContext(options, currentUser.Object, dateTime.Object);
    }

    [Fact]
    public async Task CreateCategory_GeneratesSlug()
    {
        await using var db = CreateDbContext();
        var handler = new CreateCategoryCommandHandler(db);

        var result = await handler.Handle(
            new CreateCategoryCommand("Mỹ phẩm", "Skincare", null, 1, true),
            CancellationToken.None);

        result.Name.Should().Be("Mỹ phẩm");
        result.Slug.Should().NotBeNullOrWhiteSpace();
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_AssignsCurrentUserAsSeller()
    {
        var sellerId = Guid.NewGuid();
        await using var db = CreateDbContext(sellerId);

        var category = await new CreateCategoryCommandHandler(db).Handle(
            new CreateCategoryCommand("Thời trang", null, "thoi-trang", 1, true),
            CancellationToken.None);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(sellerId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Role).Returns("User");

        var handler = new CreateProductCommandHandler(db, currentUser.Object);

        var result = await handler.Handle(
            new CreateProductCommand(
                "Áo khoác",
                "Còn mới 95%",
                900000,
                450000,
                ProductCondition.LikeNew,
                category.Id,
                "Quận 1, TP.HCM",
                1,
                null,
                null,
                AcceptedPaymentOption.CashOnDelivery,
                new[] { DeliverySpeed.Standard },
                ProductStatus.Available),
            CancellationToken.None);

        result.SellerId.Should().Be(sellerId);
        result.Status.Should().Be(ProductStatus.Available.ToString());
        result.Name.Should().Be("Áo khoác");
    }
}
