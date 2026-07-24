namespace PassDo.Application.Common.Interfaces;

public interface IPresenceTracker
{
    Task TouchAsync(Guid userId, CancellationToken cancellationToken = default);
}

