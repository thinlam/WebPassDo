using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Interfaces;
using System.Security.Claims;

namespace PassDo.Api.Hubs;

[Authorize]
public class PresenceHub : Hub
{
    private readonly IPresenceTracker _tracker;
    private readonly IApplicationDbContext _db;

    public PresenceHub(IPresenceTracker tracker, IApplicationDbContext db)
    {
        _tracker = tracker;
        _db = db;
    }

    private Guid? UserId =>
        Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub"), out var id)
            ? id : null;

    public override async Task OnConnectedAsync()
    {
        if (UserId is Guid uid)
        {
            await _tracker.TouchAsync(uid);
            await Clients.Others.SendAsync("PresenceChanged", new
            {
                userId = uid,
                isOnline = true,
                lastSeenAt = DateTime.UtcNow
            });
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (UserId is Guid uid)
        {
            await _tracker.TouchAsync(uid);
            // Clients will flip to offline after 45s without heartbeats; still emit lastSeen
            await Clients.Others.SendAsync("PresenceChanged", new
            {
                userId = uid,
                isOnline = false,
                lastSeenAt = DateTime.UtcNow
            });
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task Heartbeat()
    {
        if (UserId is not Guid uid) return;
        await _tracker.TouchAsync(uid);
    }

    public async Task JoinConversation(Guid conversationId)
    {
        if (UserId is not Guid uid) return;
        var ok = await _db.Conversations.AnyAsync(c =>
            c.Id == conversationId && (c.BuyerId == uid || c.SellerId == uid));
        if (!ok) throw new HubException("Not a participant.");
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task StartTyping(Guid conversationId)
    {
        if (UserId is not Guid uid) return;
        var ok = await _db.Conversations.AnyAsync(c =>
            c.Id == conversationId && (c.BuyerId == uid || c.SellerId == uid));
        if (!ok) return;
        await Clients.OthersInGroup(GroupName(conversationId))
            .SendAsync("TypingStarted", new { conversationId, userId = uid });
    }

    public async Task StopTyping(Guid conversationId)
    {
        if (UserId is not Guid uid) return;
        var ok = await _db.Conversations.AnyAsync(c =>
            c.Id == conversationId && (c.BuyerId == uid || c.SellerId == uid));
        if (!ok) return;
        await Clients.OthersInGroup(GroupName(conversationId))
            .SendAsync("TypingStopped", new { conversationId, userId = uid });
    }

    private static string GroupName(Guid id) => $"conversation:{id}";
}

