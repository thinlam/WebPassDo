using PassDo.Application.Common.Interfaces;
using PassDo.Application.Notifications.DTOs;

namespace PassDo.Infrastructure.Services;

/// <summary>No-op publisher used when SignalR host is unavailable (e.g. unit tests).</summary>
public class NullNotificationRealtimePublisher : INotificationRealtimePublisher
{
    public Task PublishAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
