using PassDo.Application.Notifications.DTOs;

namespace PassDo.Application.Common.Interfaces;

public interface INotificationRealtimePublisher
{
    Task PublishAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default);
}
