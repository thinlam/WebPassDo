using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;

namespace PassDo.Application.Chat;

public record GetConversationMessagesQuery(Guid ConversationId, DateTime? After = null) : IRequest<IReadOnlyList<MessageDto>>;

public class GetConversationMessagesQueryHandler : IRequestHandler<GetConversationMessagesQuery, IReadOnlyList<MessageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetConversationMessagesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MessageDto>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var userId = _currentUser.UserId.Value;

        var conversation = await _context.Conversations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation", request.ConversationId);

        if (conversation.BuyerId != userId && conversation.SellerId != userId)
        {
            throw new ForbiddenException("You are not a participant of this conversation.");
        }

        var query = _context.Messages.AsNoTracking()
            .Include(x => x.Sender)
            .Where(x => x.ConversationId == request.ConversationId);

        if (request.After.HasValue)
        {
            query = query.Where(x => x.CreatedAt > request.After.Value);
        }

        var messages = await query
            .OrderBy(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return messages.Select(m => new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderName = m.Sender?.FullName ?? string.Empty,
            Content = m.Content,
            IsRead = m.IsRead,
            CreatedAt = m.CreatedAt
        }).ToList();
    }
}
