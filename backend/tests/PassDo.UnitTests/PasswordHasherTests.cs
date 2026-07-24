using FluentAssertions;
using PassDo.Infrastructure.Identity;

namespace PassDo.UnitTests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_And_Verify_Works()
    {
        var hasher = new PasswordHasherService();
        var hash = hasher.Hash("Admin@123456");

        hasher.Verify("Admin@123456", hash).Should().BeTrue();
        hasher.Verify("WrongPassword", hash).Should().BeFalse();
    }
}
