namespace PassDo.Application.Presence;

public static class PresenceRules
{
    public static readonly TimeSpan OnlineThreshold = TimeSpan.FromSeconds(45);

    public static bool IsOnline(DateTime? lastSeenAt, DateTime utcNow)
    {
        if (lastSeenAt is null) return false;
        var seen = lastSeenAt.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(lastSeenAt.Value, DateTimeKind.Utc)
            : lastSeenAt.Value.ToUniversalTime();
        return utcNow - seen < OnlineThreshold;
    }

    public static string? FormatLastActive(DateTime? lastSeenAt, DateTime utcNow)
    {
        if (lastSeenAt is null) return null;
        var seen = lastSeenAt.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(lastSeenAt.Value, DateTimeKind.Utc)
            : lastSeenAt.Value.ToUniversalTime();
        var delta = utcNow - seen;
        if (delta < TimeSpan.Zero) delta = TimeSpan.Zero;

        if (delta.TotalMinutes < 60)
        {
            var m = Math.Max(1, (int)Math.Floor(delta.TotalMinutes));
            return $"Hoạt động {m} phút trước";
        }
        if (delta.TotalHours < 24)
        {
            var h = Math.Max(1, (int)Math.Floor(delta.TotalHours));
            return $"Hoạt động {h} giờ trước";
        }
        var d = Math.Max(1, (int)Math.Floor(delta.TotalDays));
        return $"Hoạt động {d} ngày trước";
    }
}
