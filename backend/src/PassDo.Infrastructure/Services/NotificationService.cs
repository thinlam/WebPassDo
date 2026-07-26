using PassDo.Application.Common.Interfaces;
using PassDo.Application.Notifications.DTOs;
using PassDo.Domain.Entities;

namespace PassDo.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationRealtimePublisher _realtime;

    public NotificationService(IApplicationDbContext context, INotificationRealtimePublisher realtime)
    {
        _context = context;
        _realtime = realtime;
    }

    public async Task NotifyAsync(
        Guid userId,
        string type,
        string title,
        string content,
        Guid? relatedEntityId,
        string? relatedEntityType,
        string? actionUrl,
        CancellationToken cancellationToken)
    {
        var entity = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Content = content,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            ActionUrl = actionUrl,
            IsRead = false
        };

        _context.Notifications.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _realtime.PublishAsync(
            userId,
            new NotificationDto
            {
                Id = entity.Id,
                Type = entity.Type,
                Title = entity.Title,
                Content = entity.Content,
                RelatedEntityId = entity.RelatedEntityId,
                RelatedEntityType = entity.RelatedEntityType,
                ActionUrl = entity.ActionUrl,
                IsRead = entity.IsRead,
                ReadAt = entity.ReadAt,
                CreatedAt = entity.CreatedAt
            },
            cancellationToken);
    }
}
