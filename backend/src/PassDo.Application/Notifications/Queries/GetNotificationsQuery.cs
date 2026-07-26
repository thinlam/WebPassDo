using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Models;
using PassDo.Application.Notifications.DTOs;

namespace PassDo.Application.Notifications.Queries;

public record GetNotificationsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<NotificationDto>>;
public record GetUnreadNotificationCountQuery() : IRequest<int>;

public class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class NotificationQueryHandlers :
    IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>,
    IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public NotificationQueryHandlers(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();

        var query = _context.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Type = x.Type,
                Title = x.Title,
                Content = x.Content,
                RelatedEntityId = x.RelatedEntityId,
                RelatedEntityType = x.RelatedEntityType,
                ActionUrl = x.ActionUrl,
                IsRead = x.IsRead,
                ReadAt = x.ReadAt,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<NotificationDto>.Create(items, request.Page, request.PageSize, totalItems);
    }

    public async Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        return await _context.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);
    }

    private Guid RequireUser() => _currentUser.UserId ?? throw new UnauthorizedException();
}
