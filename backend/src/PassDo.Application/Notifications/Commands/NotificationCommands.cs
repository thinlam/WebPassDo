using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;

namespace PassDo.Application.Notifications.Commands;

public record MarkNotificationReadCommand(Guid Id) : IRequest;
public record MarkAllNotificationsReadCommand() : IRequest;

public class NotificationCommandHandlers :
    IRequestHandler<MarkNotificationReadCommand>,
    IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public NotificationCommandHandlers(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var entity = await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Notification", request.Id);

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            entity.ReadAt = _dateTimeProvider.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var unread = await _context.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        foreach (var item in unread)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private Guid RequireUser() => _currentUser.UserId ?? throw new UnauthorizedException();
}
