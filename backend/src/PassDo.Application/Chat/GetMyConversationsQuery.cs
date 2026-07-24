using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Presence;

namespace PassDo.Application.Chat;

public record GetMyConversationsQuery : IRequest<IReadOnlyList<ConversationDto>>;

public class GetMyConversationsQueryHandler : IRequestHandler<GetMyConversationsQuery, IReadOnlyList<ConversationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyConversationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ConversationDto>> Handle(GetMyConversationsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var userId = _currentUser.UserId.Value;

        var conversations = await _context.Conversations
            .AsNoTracking()
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .Include(x => x.Product).ThenInclude(p => p.Images)
            .Include(x => x.Messages)
            .Where(x => x.BuyerId == userId || x.SellerId == userId)
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        return conversations.Select(c =>
        {
            var other = c.BuyerId == userId ? c.Seller : c.Buyer;
            var otherId = c.BuyerId == userId ? c.SellerId : c.BuyerId;
            var lastMsg = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            var unread = c.Messages.Count(m => !m.IsRead && m.SenderId != userId);

            return new ConversationDto
            {
                Id = c.Id,
                ProductId = c.ProductId,
                ProductName = c.Product?.Name ?? string.Empty,
                ProductImageUrl = c.Product?.Images
                    .OrderByDescending(x => x.IsPrimary)
                    .ThenBy(x => x.DisplayOrder)
                    .Select(x => x.Url)
                    .FirstOrDefault(),
                BuyerId = c.BuyerId,
                BuyerName = c.Buyer?.FullName ?? string.Empty,
                SellerId = c.SellerId,
                SellerName = c.Seller?.FullName ?? string.Empty,
                OtherUserId = otherId,
                OtherUserName = other?.FullName ?? string.Empty,
                OtherUserLastSeenAt = other?.LastSeenAt,
                OtherUserIsOnline = PresenceRules.IsOnline(other?.LastSeenAt, DateTime.UtcNow),
                LastMessageAt = c.LastMessageAt,
                LastMessagePreview = lastMsg?.Content?.Length > 100
                    ? lastMsg.Content[..100] + "..."
                    : lastMsg?.Content,
                UnreadCount = unread,
                CreatedAt = c.CreatedAt
            };
        }).ToList();
    }
}
