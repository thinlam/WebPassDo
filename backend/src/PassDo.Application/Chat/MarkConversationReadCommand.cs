using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;

namespace PassDo.Application.Chat;

public record MarkConversationReadCommand(Guid ConversationId) : IRequest;

public class MarkConversationReadCommandHandler : IRequestHandler<MarkConversationReadCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MarkConversationReadCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(MarkConversationReadCommand request, CancellationToken cancellationToken)
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

        var unread = await _context.Messages
            .Where(x => x.ConversationId == request.ConversationId && x.SenderId != userId && !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var msg in unread)
        {
            msg.IsRead = true;
        }

        if (unread.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
