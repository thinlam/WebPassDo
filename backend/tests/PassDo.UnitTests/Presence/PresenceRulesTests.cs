using FluentAssertions;
using PassDo.Application.Presence;

namespace PassDo.UnitTests.Presence;

public class PresenceRulesTests
{
    [Fact]
    public void IsOnline_WhenWithin45Seconds_ReturnsTrue()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        PresenceRules.IsOnline(now.AddSeconds(-20), now).Should().BeTrue();
    }

    [Fact]
    public void IsOnline_WhenOlderThan45Seconds_ReturnsFalse()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        PresenceRules.IsOnline(now.AddSeconds(-46), now).Should().BeFalse();
    }

    [Fact]
    public void FormatLastActive_Null_ReturnsNull()
    {
        PresenceRules.FormatLastActive(null, DateTime.UtcNow).Should().BeNull();
    }

    [Fact]
    public void FormatLastActive_Minutes_UsesVietnamesePhrase()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        PresenceRules.FormatLastActive(now.AddMinutes(-5), now)
            .Should().Be("Hoạt động 5 phút trước");
    }

    [Fact]
    public void FormatLastActive_Hours_UsesVietnamesePhrase()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        PresenceRules.FormatLastActive(now.AddHours(-2), now)
            .Should().Be("Hoạt động 2 giờ trước");
    }

    [Fact]
    public void FormatLastActive_Days_UsesVietnamesePhrase()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        PresenceRules.FormatLastActive(now.AddDays(-3), now)
            .Should().Be("Hoạt động 3 ngày trước");
    }
}
