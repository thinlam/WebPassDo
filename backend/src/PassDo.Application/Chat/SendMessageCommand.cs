using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Domain.Constants;
using PassDo.Domain.Entities;

namespace PassDo.Application.Chat;

public record SendMessageCommand(Guid ConversationId, string Content) : IRequest<MessageDto>;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
    }
}

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationService _notifications;

    public SendMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _notifications = notifications;
    }

    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var userId = _currentUser.UserId.Value;

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(x => x.Id == request.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation", request.ConversationId);

        if (conversation.BuyerId != userId && conversation.SellerId != userId)
        {
            throw new ForbiddenException("You are not a participant of this conversation.");
        }

        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = userId,
            Content = request.Content.Trim()
        };

        _context.Messages.Add(message);
        conversation.LastMessageAt = _clock.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var sender = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        var recipientId = conversation.BuyerId == userId
            ? conversation.SellerId
            : conversation.BuyerId;

        var preview = message.Content.Length > 120
            ? message.Content[..117] + "..."
            : message.Content;

        var senderName = sender?.FullName ?? "Ai đó";

        await _notifications.NotifyAsync(
            recipientId,
            NotificationTypes.NewMessage,
            $"Tin nhắn mới từ {senderName}",
            preview,
            conversation.Id,
            "Conversation",
            $"/messages/{conversation.Id}",
            cancellationToken);

        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = senderName,
            Content = message.Content,
            IsRead = false,
            CreatedAt = message.CreatedAt
        };
    }
}
