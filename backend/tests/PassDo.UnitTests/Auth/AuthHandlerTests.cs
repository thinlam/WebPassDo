using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PassDo.Application.Auth.Commands.Login;
using PassDo.Application.Auth.Commands.Register;
using PassDo.Application.Auth.Services;
using PassDo.Application.Common.Interfaces;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;
using PassDo.Infrastructure.Identity;
using PassDo.Infrastructure.Persistence;
using PassDo.Infrastructure.Services;

namespace PassDo.UnitTests.Auth;

public class AuthHandlerTests
{
    private static PassDoDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PassDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var currentUser = new Mock<ICurrentUserService>();
        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        return new PassDoDbContext(options, currentUser.Object, dateTime.Object);
    }

    [Fact]
    public async Task Register_CreatesUser_WithUserRole()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasherService();
        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        jwt.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        jwt.Setup(x => x.GetRefreshTokenExpiry()).Returns(DateTime.UtcNow.AddDays(7));

        var sessionService = new AuthSessionService(dbContext, jwt.Object, new DateTimeProvider());
        var handler = new RegisterCommandHandler(dbContext, passwordHasher, sessionService);

        var result = await handler.Handle(
            new RegisterCommand("user@test.com", "Password123", "Test User", null, "127.0.0.1"),
            CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.User.Email.Should().Be("user@test.com");
        result.User.Role.Should().Be(UserRole.User.ToString());

        var savedUser = await dbContext.Users.FirstAsync();
        savedUser.Role.Should().Be(UserRole.User);
        passwordHasher.Verify("Password123", savedUser.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Login_ReturnsTokens_ForValidCredentials()
    {
        await using var dbContext = CreateDbContext();
        var passwordHasher = new PasswordHasherService();
        var user = new User
        {
            Email = "user@test.com",
            FullName = "Test User",
            PasswordHash = passwordHasher.Hash("Password123"),
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        jwt.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        jwt.Setup(x => x.GetRefreshTokenExpiry()).Returns(DateTime.UtcNow.AddDays(7));

        var sessionService = new AuthSessionService(dbContext, jwt.Object, new DateTimeProvider());
        var handler = new LoginCommandHandler(dbContext, passwordHasher, sessionService);

        var result = await handler.Handle(
            new LoginCommand("user@test.com", "Password123", "127.0.0.1"),
            CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.User.Email.Should().Be("user@test.com");
    }
}
