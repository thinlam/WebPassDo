namespace PassDo.Application.Chat;

public class ConversationDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public Guid BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public bool OtherUserIsOnline { get; set; }
    public DateTime? OtherUserLastSeenAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public int UnreadCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
