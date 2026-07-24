using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Presence;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;

namespace PassDo.Application.Chat;

public record StartOrGetConversationCommand(Guid ProductId) : IRequest<ConversationDto>;

public class StartOrGetConversationCommandValidator : AbstractValidator<StartOrGetConversationCommand>
{
    public StartOrGetConversationCommandValidator() => RuleFor(x => x.ProductId).NotEmpty();
}

public class StartOrGetConversationCommandHandler : IRequestHandler<StartOrGetConversationCommand, ConversationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public StartOrGetConversationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ConversationDto> Handle(StartOrGetConversationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var buyerId = _currentUser.UserId.Value;

        var product = await _context.Products.AsNoTracking()
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        if (product.SellerId == buyerId)
        {
            throw new ConflictException("You cannot start a conversation on your own product.");
        }

        var existing = await _context.Conversations
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .Include(x => x.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(x =>
                x.ProductId == request.ProductId
                && x.BuyerId == buyerId
                && x.SellerId == product.SellerId, cancellationToken);

        if (existing is not null)
        {
            return MapConversation(existing, buyerId);
        }

        var conversation = new Conversation
        {
            ProductId = product.Id,
            BuyerId = buyerId,
            SellerId = product.SellerId
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);

        var loaded = await _context.Conversations
            .AsNoTracking()
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .Include(x => x.Product).ThenInclude(p => p.Images)
            .FirstAsync(x => x.Id == conversation.Id, cancellationToken);

        return MapConversation(loaded, buyerId);
    }

    private static ConversationDto MapConversation(Conversation c, Guid currentUserId)
    {
        var other = c.BuyerId == currentUserId ? c.Seller : c.Buyer;
        var otherId = c.BuyerId == currentUserId ? c.SellerId : c.BuyerId;

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
            CreatedAt = c.CreatedAt
        };
    }
}
