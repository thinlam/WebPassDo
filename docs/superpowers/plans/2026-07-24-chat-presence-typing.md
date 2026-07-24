# Chat Presence & Typing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add SignalR-backed online/offline presence and typing indicators across conversation, messages list, and product detail seller UI.

**Architecture:** Persist `User.LastSeenAt` and refresh it via authenticated SignalR hub heartbeats; broadcast presence and typing over hub groups; REST supplies first paint / fallback. Frontend connects one hub when logged in and renders a shared `PresenceLabel`.

**Tech Stack:** .NET 8 SignalR + JWT, EF Core migration, React 19, `@microsoft/signalr`, TanStack Query, nginx WebSocket proxy.

## Global Constraints

- Online copy: `Đang online`
- Offline copy: `Hoạt động X phút trước` / `X giờ trước` / `X ngày trước` (never Zalo-style `1p`)
- Typing copy: `Đang nhập...` (overrides presence text in conversation)
- Online window: `LastSeenAt` within **45 seconds** (UTC)
- Client heartbeat: ~**20 seconds**
- Typing idle clear: ~**3 seconds** without refresh; send StopTyping after ~**2–3s** idle or on send
- Surfaces: Conversation header, Messages list, Product detail seller block
- Hub path: `/hubs/presence`
- Do not add Redis backplane in this plan
- Only commit when the user explicitly asks (skip plan commit steps unless requested)

## File structure

| File | Responsibility |
|------|----------------|
| `backend/src/PassDo.Domain/Entities/User.cs` | Add `LastSeenAt` |
| `backend/.../Migrations/*_AddUserLastSeenAt.cs` | Schema |
| `backend/src/PassDo.Application/Presence/PresenceRules.cs` | Pure `IsOnline` + relative Vietnamese text helpers used by API/tests |
| `backend/src/PassDo.Application/Presence/PresenceDtos.cs` | `PresenceDto` |
| `backend/src/PassDo.Application/Presence/GetUserPresenceQuery.cs` | REST presence query |
| `backend/src/PassDo.Application/Presence/IPresenceTracker.cs` + Infrastructure impl | Update `LastSeenAt`, optional in-memory connection count |
| `backend/src/PassDo.Api/Hubs/PresenceHub.cs` | Heartbeat, join/leave conversation, typing |
| `backend/src/PassDo.Infrastructure/DependencyInjection.cs` | JWT query token for hubs |
| `backend/src/PassDo.Api/Program.cs` | `AddSignalR`, `MapHub`, CORS credentials |
| `backend/.../Chat/ChatDtos.cs` + mappers | `OtherUser*` + presence fields aligned with frontend |
| `backend/.../Products/DTOs/ProductDtos.cs` + GetProductById | `SellerIsOnline`, `SellerLastSeenAt` |
| `backend/.../Controllers/UsersController.cs` | `GET {id}/presence` |
| `frontend/nginx.conf` + `vite.config.ts` | Proxy `/hubs` with WebSocket |
| `frontend/src/lib/presence.ts` | Format relative activity + online check (45s) |
| `frontend/src/components/presence/PresenceLabel.tsx` | UI label |
| `frontend/src/features/presence/usePresenceHub.ts` | Single hub connection + events |
| `frontend/src/features/presence/api.ts` | REST presence fetch |
| Pages: Conversation / Messages / ProductDetail | Wire labels + typing |

---

### Task 1: LastSeenAt + PresenceRules + unit tests

**Files:**
- Modify: `backend/src/PassDo.Domain/Entities/User.cs`
- Create: `backend/src/PassDo.Application/Presence/PresenceRules.cs`
- Create: `backend/tests/PassDo.UnitTests/Presence/PresenceRulesTests.cs`
- Create migration via `dotnet ef` under `backend/src/PassDo.Infrastructure`

**Interfaces:**
- Produces: `PresenceRules.OnlineThreshold` = 45s; `PresenceRules.IsOnline(DateTime? lastSeenAt, DateTime utcNow)`; `PresenceRules.FormatLastActive(DateTime? lastSeenAt, DateTime utcNow)` → `string?` (`null` if never seen)

- [ ] **Step 1: Write failing unit tests**

```csharp
using FluentAssertions;
using PassDo.Application.Presence;

namespace PassDo.UnitTests.Presence;

public class PresenceRulesTests
{
    [Fact]
    public void IsOnline_WhenWithin45Seconds_ReturnsTrue()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        PresenceRules.IsOnline(now.AddSeconds(-20), now).Should().BeTrue();
    }

    [Fact]
    public void IsOnline_WhenOlderThan45Seconds_ReturnsFalse()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        PresenceRules.IsOnline(now.AddSeconds(-46), now).Should().BeFalse();
    }

    [Fact]
    public void FormatLastActive_Null_ReturnsNull()
    {
        PresenceRules.FormatLastActive(null, DateTime.UtcNow).Should().BeNull();
    }

    [Fact]
    public void FormatLastActive_Minutes_UsesVietnamesePhrase()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        PresenceRules.FormatLastActive(now.AddMinutes(-5), now)
            .Should().Be("Hoạt động 5 phút trước");
    }

    [Fact]
    public void FormatLastActive_Hours_UsesVietnamesePhrase()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        PresenceRules.FormatLastActive(now.AddHours(-2), now)
            .Should().Be("Hoạt động 2 giờ trước");
    }

    [Fact]
    public void FormatLastActive_Days_UsesVietnamesePhrase()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        PresenceRules.FormatLastActive(now.AddDays(-3), now)
            .Should().Be("Hoạt động 3 ngày trước");
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL** (type missing)

```bash
cd e:\WebPassDo\backend
dotnet test tests/PassDo.UnitTests/PassDo.UnitTests.csproj --filter FullyQualifiedName~PresenceRulesTests
```

Expected: compile error or FAIL finding `PresenceRules`.

- [ ] **Step 3: Implement domain + rules**

Add to `User.cs`:

```csharp
public DateTime? LastSeenAt { get; set; }
```

Create `PresenceRules.cs`:

```csharp
namespace PassDo.Application.Presence;

public static class PresenceRules
{
    public static readonly TimeSpan OnlineThreshold = TimeSpan.FromSeconds(45);

    public static bool IsOnline(DateTime? lastSeenAt, DateTime utcNow)
    {
        if (lastSeenAt is null) return false;
        var seen = lastSeenAt.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(lastSeenAt.Value, DateTimeKind.Utc)
            : lastSeenAt.Value.ToUniversalTime();
        return utcNow - seen < OnlineThreshold;
    }

    public static string? FormatLastActive(DateTime? lastSeenAt, DateTime utcNow)
    {
        if (lastSeenAt is null) return null;
        var seen = lastSeenAt.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(lastSeenAt.Value, DateTimeKind.Utc)
            : lastSeenAt.Value.ToUniversalTime();
        var delta = utcNow - seen;
        if (delta < TimeSpan.Zero) delta = TimeSpan.Zero;

        if (delta.TotalMinutes < 60)
        {
            var m = Math.Max(1, (int)Math.Floor(delta.TotalMinutes));
            return $"Hoạt động {m} phút trước";
        }
        if (delta.TotalHours < 24)
        {
            var h = Math.Max(1, (int)Math.Floor(delta.TotalHours));
            return $"Hoạt động {h} giờ trước";
        }
        var d = Math.Max(1, (int)Math.Floor(delta.TotalDays));
        return $"Hoạt động {d} ngày trước";
    }
}
```

- [ ] **Step 4: Add EF migration**

```bash
cd e:\WebPassDo\backend\src\PassDo.Api
dotnet ef migrations add AddUserLastSeenAt --project ../PassDo.Infrastructure --startup-project .
```

- [ ] **Step 5: Run tests — expect PASS**

```bash
dotnet test tests/PassDo.UnitTests/PassDo.UnitTests.csproj --filter FullyQualifiedName~PresenceRulesTests
```

Expected: all PASS.

- [ ] **Step 6: Commit** (only if user asked)

```bash
git add backend/src/PassDo.Domain/Entities/User.cs backend/src/PassDo.Application/Presence backend/src/PassDo.Infrastructure/Persistence/Migrations backend/tests/PassDo.UnitTests/Presence
git commit -m "feat(presence): add LastSeenAt and PresenceRules"
```

---

### Task 2: Presence tracker + SignalR hub + JWT + CORS

**Files:**
- Create: `backend/src/PassDo.Application/Common/Interfaces/IPresenceTracker.cs`
- Create: `backend/src/PassDo.Infrastructure/Presence/PresenceTracker.cs`
- Create: `backend/src/PassDo.Api/Hubs/PresenceHub.cs`
- Modify: `backend/src/PassDo.Infrastructure/DependencyInjection.cs` (JWT `OnMessageReceived` for `/hubs`)
- Modify: `backend/src/PassDo.Api/Program.cs` (`AddSignalR`, `MapHub`, CORS `AllowCredentials`)
- Modify: `backend/src/PassDo.Infrastructure/DependencyInjection.cs` register `IPresenceTracker`

**Interfaces:**
- Consumes: `PresenceRules`, `IApplicationDbContext`, `ICurrentUserService` / hub `Context.User`
- Produces hub client methods:
  - Server: `Heartbeat()`, `JoinConversation(Guid conversationId)`, `LeaveConversation(Guid conversationId)`, `StartTyping(Guid conversationId)`, `StopTyping(Guid conversationId)`
  - Client events: `PresenceChanged(object)`, `TypingStarted(object)`, `TypingStopped(object)`
- `IPresenceTracker.TouchAsync(Guid userId, CancellationToken ct)` updates `LastSeenAt = UtcNow` and SaveChanges

- [ ] **Step 1: Add interface + tracker**

```csharp
// IPresenceTracker.cs
namespace PassDo.Application.Common.Interfaces;

public interface IPresenceTracker
{
    Task TouchAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

```csharp
// PresenceTracker.cs
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Interfaces;
using PassDo.Infrastructure.Persistence;

namespace PassDo.Infrastructure.Presence;

public class PresenceTracker : IPresenceTracker
{
    private readonly PassDoDbContext _db;

    public PresenceTracker(PassDoDbContext db) => _db = db;

    public async Task TouchAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return;
        user.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
```

Register in DI: `services.AddScoped<IPresenceTracker, PresenceTracker>();`

- [ ] **Step 2: JWT for SignalR query token**

Inside existing `AddJwtBearer` options in `DependencyInjection.cs`, add:

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};
```

(Add `using Microsoft.AspNetCore.Authentication.JwtBearer;`)

- [ ] **Step 3: Implement PresenceHub**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Presence;
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
        await Clients.Others.SendAsync("PresenceChanged", new
        {
            userId = uid,
            isOnline = true,
            lastSeenAt = DateTime.UtcNow
        });
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
        await Clients.OthersInGroup(GroupName(conversationId))
            .SendAsync("TypingStopped", new { conversationId, userId = uid });
    }

    private static string GroupName(Guid id) => $"conversation:{id}";
}
```

Note: verify claim type used elsewhere in `ICurrentUserService` / JWT generation and use the **same** claim for `UserId` in the hub.

- [ ] **Step 4: Wire Program.cs**

- `builder.Services.AddSignalR();`
- Change CORS policy to `.AllowCredentials()` (keep `WithOrigins`, no `AllowAnyOrigin`)
- After auth middleware: `app.MapHub<PresenceHub>("/hubs/presence");`

- [ ] **Step 5: Smoke compile**

```bash
cd e:\WebPassDo\backend
dotnet build src/PassDo.Api/PassDo.Api.csproj
```

Expected: Build succeeded.

- [ ] **Step 6: Commit** (only if user asked)

---

### Task 3: REST presence + DTO extensions

**Files:**
- Create: `backend/src/PassDo.Application/Presence/PresenceDtos.cs`
- Create: `backend/src/PassDo.Application/Presence/GetUserPresenceQuery.cs`
- Modify: `backend/src/PassDo.Api/Controllers/UsersController.cs`
- Modify: `backend/src/PassDo.Application/Chat/ChatDtos.cs`
- Modify: `backend/src/PassDo.Application/Chat/GetMyConversationsQuery.cs`
- Modify: `backend/src/PassDo.Application/Chat/StartOrGetConversationCommand.cs`
- Modify: `backend/src/PassDo.Application/Products/DTOs/ProductDtos.cs`
- Modify: `backend/src/PassDo.Application/Products/Queries/GetProductById/GetProductByIdQuery.cs`

**Interfaces:**
- Produces REST: `GET /api/users/{id}/presence` → `ApiResponse<PresenceDto>` where `PresenceDto { bool IsOnline; DateTime? LastSeenAt; }`
- ConversationDto adds: `OtherUserId`, `OtherUserName`, `OtherUserIsOnline`, `OtherUserLastSeenAt`, keep existing buyer/seller fields; map `LastMessagePreview` ↔ frontend `lastMessage`
- ProductDto adds: `bool SellerIsOnline`, `DateTime? SellerLastSeenAt`
- Presence endpoint: `[AllowAnonymous]` so product page guests can read seller status

- [ ] **Step 1: PresenceDto + query + controller action**

```csharp
public class PresenceDto
{
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
```

Handler loads user by id; if missing → NotFound; else set `IsOnline = PresenceRules.IsOnline(user.LastSeenAt, DateTime.UtcNow)`.

Controller:

```csharp
[AllowAnonymous]
[HttpGet("{id:guid}/presence")]
public async Task<ActionResult<ApiResponse<PresenceDto>>> GetPresence(Guid id)
{
    var result = await _mediator.Send(new GetUserPresenceQuery(id));
    return Ok(ApiResponse<PresenceDto>.Ok(result));
}
```

- [ ] **Step 2: Extend ConversationDto mapping**

In both chat handlers, for `currentUserId`:

```csharp
var other = c.BuyerId == currentUserId ? c.Seller : c.Buyer;
var otherId = c.BuyerId == currentUserId ? c.SellerId : c.BuyerId;
// ...
OtherUserId = otherId,
OtherUserName = other?.FullName ?? string.Empty,
OtherUserLastSeenAt = other?.LastSeenAt,
OtherUserIsOnline = PresenceRules.IsOnline(other?.LastSeenAt, DateTime.UtcNow),
LastMessagePreview = ... // existing
```

Ensure `Include` loads Buyer/Seller (already does).

- [ ] **Step 3: Product detail seller presence**

After loading seller in `GetProductByIdQueryHandler`:

```csharp
dto.SellerIsOnline = PresenceRules.IsOnline(seller?.LastSeenAt, DateTime.UtcNow);
dto.SellerLastSeenAt = seller?.LastSeenAt;
```

- [ ] **Step 4: Build + optional manual curl**

```bash
dotnet build src/PassDo.Api/PassDo.Api.csproj
```

- [ ] **Step 5: Commit** (only if user asked)

---

### Task 4: Frontend presence lib + PresenceLabel + hub hook + proxies

**Files:**
- Modify: `frontend/package.json` — add `@microsoft/signalr`
- Create: `frontend/src/lib/presence.ts`
- Create: `frontend/src/components/presence/PresenceLabel.tsx`
- Create: `frontend/src/features/presence/api.ts`
- Create: `frontend/src/features/presence/usePresenceHub.ts`
- Create: `frontend/src/features/presence/PresenceProvider.tsx` (optional thin wrapper)
- Modify: `frontend/src/routes/index.tsx` or layout that wraps authenticated app — mount hub when auth
- Modify: `frontend/nginx.conf` — `/hubs/` WebSocket proxy
- Modify: `frontend/vite.config.ts` — proxy `/hubs` with `ws: true`
- Modify: `frontend/src/types/index.ts` — Conversation + Product presence fields

**Interfaces:**
- `formatLastActive(lastSeenAt: string | null | undefined, now = Date): string | null` — mirror backend phrases
- `isOnline(lastSeenAt, now = Date, thresholdMs = 45000): boolean`
- `PresenceLabel({ isOnline, lastSeenAt, isTyping?: boolean })`
- `usePresenceHub()` returns `{ connectionState, joinConversation, leaveConversation, startTyping, stopTyping, subscribePresence, subscribeTyping }`
- Hub URL: relative `/hubs/presence` (works via nginx/vite)

- [ ] **Step 1: Install dependency**

```bash
cd e:\WebPassDo\frontend
npm install @microsoft/signalr
```

- [ ] **Step 2: Implement `lib/presence.ts` + `PresenceLabel`**

```tsx
// PresenceLabel.tsx (sketch)
export function PresenceLabel({ isOnline, lastSeenAt, isTyping }: {
  isOnline?: boolean
  lastSeenAt?: string | null
  isTyping?: boolean
}) {
  if (isTyping) return <p className="text-sm text-forest">Đang nhập...</p>
  if (isOnline) return (
    <p className="flex items-center gap-1.5 text-sm text-forest">
      <span className="inline-block h-2 w-2 rounded-full bg-emerald-500" />
      Đang online
    </p>
  )
  const text = formatLastActive(lastSeenAt)
  if (!text) return null
  return <p className="text-sm text-muted">{text}</p>
}
```

- [ ] **Step 3: Hub hook**

- Build connection with `HubConnectionBuilder().withUrl('/hubs/presence', { accessTokenFactory: () => token }).withAutomaticReconnect().build()`
- On start: interval Heartbeat every 20s
- Cleanup on logout/unmount
- Read token from `useAuthStore`

- [ ] **Step 4: Proxy**

`nginx.conf`:

```nginx
location /hubs/ {
    proxy_pass http://backend:8080/hubs/;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_read_timeout 3600s;
}
```

`vite.config.ts` proxy:

```ts
'/hubs': {
  target: 'http://localhost:8081', // or 8080 if local API port
  changeOrigin: true,
  ws: true,
},
```

Align target with how local API is usually run (Docker API is **8081** on host; container internal **8080**).

- [ ] **Step 5: Update types**

```ts
export type Conversation = {
  id: string
  productId?: string
  otherUserId: string
  otherUserName: string
  otherUserAvatarUrl?: string | null
  otherUserIsOnline?: boolean
  otherUserLastSeenAt?: string | null
  lastMessage?: string | null
  lastMessagePreview?: string | null
  lastMessageAt?: string | null
  unreadCount: number
}

// Product adds:
sellerIsOnline?: boolean
sellerLastSeenAt?: string | null
```

Map list UI to use `lastMessage ?? lastMessagePreview`.

- [ ] **Step 6: `tsc` check**

```bash
cd e:\WebPassDo\frontend
npx tsc -b --pretty false
```

Expected: no errors from new files (pages may still need wiring).

- [ ] **Step 7: Commit** (only if user asked)

---

### Task 5: Wire ConversationPage (presence + typing)

**Files:**
- Modify: `frontend/src/pages/ConversationPage.tsx`
- Modify: `frontend/src/pages/MessagesPage.tsx` only if needed for navigating with other user id (prefer load conversations for header peer)

**Interfaces:**
- Consumes hub `joinConversation` / typing APIs from Task 4
- Needs other user id: from conversations list cache or extend messages page route state; simplest: `useQuery` conversations and find by `conversationId`, or add `GET /api/conversations/{id}` if missing — **if no get-by-id endpoint exists, derive from list query** `chatApi.listConversations()` and find matching id

- [ ] **Step 1: Add conversation header**

- Load conversations; find current; show `otherUserName` + `PresenceLabel`
- On mount: `joinConversation(id)`; unmount: `leaveConversation`
- Subscribe presence for `otherUserId` to update local `isOnline` / `lastSeenAt`
- Subscribe typing for this conversation → `isTyping`

- [ ] **Step 2: Typing on input**

- `onChange`: set local text; debounce 300ms → `startTyping(conversationId)`
- Timer 2.5s after last keystroke → `stopTyping`
- On successful send → `stopTyping` immediately

- [ ] **Step 3: Manual check** (two browsers) typing + online

- [ ] **Step 4: Commit** (only if user asked)

---

### Task 6: Wire MessagesPage + ProductDetailPage

**Files:**
- Modify: `frontend/src/pages/MessagesPage.tsx`
- Modify: `frontend/src/pages/ProductDetailPage.tsx`
- Optionally: `frontend/src/features/presence/api.ts` used on product page for refresh

- [ ] **Step 1: Messages list**

Under `otherUserName`, render:

```tsx
<PresenceLabel
  isOnline={liveOnline[conv.otherUserId] ?? conv.otherUserIsOnline}
  lastSeenAt={liveLastSeen[conv.otherUserId] ?? conv.otherUserLastSeenAt}
/>
```

Subscribe hub `PresenceChanged` to patch a small map state keyed by userId.

Also fix preview: `conv.lastMessage ?? conv.lastMessagePreview`.

- [ ] **Step 2: Product detail**

Under seller name:

```tsx
<PresenceLabel
  isOnline={sellerLive?.isOnline ?? product.sellerIsOnline}
  lastSeenAt={sellerLive?.lastSeenAt ?? product.sellerLastSeenAt}
/>
```

If authenticated, listen hub for `product.sellerId`. If guest, REST-only fields from product payload.

- [ ] **Step 3: Build frontend**

```bash
cd e:\WebPassDo\frontend
npm run build
```

Expected: success.

- [ ] **Step 4: Commit** (only if user asked)

---

### Task 7: Docker rebuild + end-to-end verification

**Files:** none new (ops)

- [ ] **Step 1: Rebuild**

```bash
cd e:\WebPassDo
docker compose up --build -d frontend backend
```

Expected: both healthy.

- [ ] **Step 2: Verify checklist from spec**

1. Two users: A online → B sees `Đang online`; A closes tab → within ~45–60s B sees `Hoạt động … trước`
2. Typing in conversation → peer sees `Đang nhập...`
3. Seller connected → buyer product detail shows online
4. Browser Network: WS to `ws://localhost:3000/hubs/presence` (or negotiate OK)
5. Unit tests still pass:

```bash
cd e:\WebPassDo\backend
dotnet test tests/PassDo.UnitTests/PassDo.UnitTests.csproj
```

- [ ] **Step 3: Commit** (only if user asked)

---

## Spec coverage self-review

| Spec requirement | Task |
|------------------|------|
| `LastSeenAt` + 45s online | 1, 2 |
| SignalR `/hubs/presence` + JWT | 2 |
| Heartbeat ~20s | 4, 5 |
| Typing Start/Stop + group auth | 2, 5 |
| REST presence + conversation/product fields | 3 |
| UI: conversation / list / product | 5, 6 |
| Copy: Đang online / Hoạt động … / Đang nhập | 1, 4 |
| nginx WebSocket | 4, 7 |
| Fallback when hub down | 3 + 4 (REST fields + label) |
| No Redis | honored |

## Placeholder / consistency check

- Hub claim type must match auth token claims (confirm in JWT issuer code during Task 2).
- Vite proxy port must match local API host port.
- Frontend Conversation mapping: backend will emit `otherUserId` / `otherUserName` / `lastMessagePreview`; UI accepts both preview field names.
