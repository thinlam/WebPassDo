using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Interfaces;
using PassDo.Infrastructure.Persistence;

namespace PassDo.Infrastructure.Presence;

public class PresenceTracker : IPresenceTracker
{
    private readonly PassDoDbContext _db;

    public PresenceTracker(PassDoDbContext db) => _db = db;

    public async Task TouchAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return;
        user.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

