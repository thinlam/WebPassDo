using Microsoft.AspNetCore.SignalR;
using PassDo.Api.Hubs;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Notifications.DTOs;

namespace PassDo.Api.Realtime;

public class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
{
    private readonly IHubContext<PresenceHub> _hub;

    public SignalRNotificationRealtimePublisher(IHubContext<PresenceHub> hub)
    {
        _hub = hub;
    }

    public Task PublishAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(PresenceHub.UserGroupName(userId))
            .SendAsync("NotificationReceived", notification, cancellationToken);
}
