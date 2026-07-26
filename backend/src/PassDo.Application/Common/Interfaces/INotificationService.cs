namespace PassDo.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(
        Guid userId,
        string type,
        string title,
        string content,
        Guid? relatedEntityId,
        string? relatedEntityType,
        string? actionUrl,
        CancellationToken cancellationToken);
}
