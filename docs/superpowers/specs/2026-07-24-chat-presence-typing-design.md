# Chat Presence & Typing Design

Date: 2026-07-24  
Status: Approved in conversation (awaiting file review)  
Scope: Online/offline presence + typing indicator for PassDo chat commerce UI

## Goal

Show realtime presence and typing so buyers and sellers feel the chat is live:

- **Online:** `Đang online` (green dot)
- **Offline:** `Hoạt động X phút trước` / `X giờ trước` / `X ngày trước`
- **Typing (conversation only):** `Đang nhập...` (overrides online/offline text)

Show status in:

1. Conversation room header (+ typing under composer area)
2. Messages list (under other party name)
3. Product detail seller block

## Decisions

| Topic | Choice |
|-------|--------|
| Surfaces | All three (chat + list + product detail) |
| Transport | SignalR realtime |
| Offline copy | Full phrase “Hoạt động … trước” (not Zalo-style `1p`) |
| Persistence | `User.LastSeenAt` in SQL + SignalR heartbeat |
| Typing storage | Ephemeral (hub only, no DB) |

## Architecture

### Data

- Add nullable `DateTime? LastSeenAt` on `User` (+ EF migration).
- Online rule: `isOnline = LastSeenAt != null && (UtcNow - LastSeenAt) < 45 seconds`.
- Heartbeat interval (client): ~20s while hub connected.
- Disconnect / no heartbeat → after ~45s peers treat user as offline; `LastSeenAt` remains for relative text.

### SignalR hub

- Endpoint: `/hubs/presence`
- Auth: JWT (`[Authorize]`); browser passes token via `accessTokenFactory` / query `access_token`.
- Client connects once when authenticated (app-level hook).

Hub methods (client → server):

- `Heartbeat()` — refresh `LastSeenAt`, optionally broadcast presence change to interested parties
- `JoinConversation(conversationId)` — join group `conversation:{id}` after verifying caller is buyer or seller
- `LeaveConversation(conversationId)`
- `StartTyping(conversationId)` / `StopTyping(conversationId)` — broadcast to others in the conversation group only

Hub events (server → client):

- `PresenceChanged({ userId, isOnline, lastSeenAt })`
- `TypingStarted({ conversationId, userId })`
- `TypingStopped({ conversationId, userId })`

### REST (fallback / initial paint)

- `GET /api/users/{id}/presence` → `{ isOnline, lastSeenAt }`
- Extend conversation DTOs with `otherUserIsOnline`, `otherUserLastSeenAt` (and keep/align `otherUserId` / `otherUserName` with frontend types)
- Extend product detail seller fields: `sellerIsOnline`, `sellerLastSeenAt`

REST remains source of truth for first paint; SignalR updates live state afterward. If hub disconnects, UI keeps last known presence and may refresh via REST without crashing.

### Nginx / Docker

Add WebSocket-capable proxy:

```nginx
location /hubs/ {
    proxy_pass http://backend:8080/hubs/;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
    proxy_read_timeout 3600s;
}
```

CORS must allow frontend origins; SignalR negotiate works through same `/hubs` path as API host (via nginx on port 3000 or direct API 8081 in local vite proxy if present).

## Frontend

### Components / hooks

- `usePresenceHub` — single connection when `isAuthenticated`; exposes subscribe helpers and typing senders.
- `PresenceLabel` — renders online / offline relative / typing override.
- Relative time helper: minutes / hours / days in Vietnamese (“Hoạt động X phút trước”, etc.). If `lastSeenAt` is null and never online, hide status line.

### Wiring

- **ConversationPage:** header with other user name + `PresenceLabel`; on input change debounce ~300ms → `StartTyping`; idle 2–3s or send message → `StopTyping`; listen for typing events for that conversation.
- **MessagesPage:** under `otherUserName`, show presence (hub updates preferred; API fields for SSR/first load).
- **ProductDetailPage:** under seller name, show seller presence (REST + hub `PresenceChanged` for that `sellerId`).

### Typing UX rules

- Typing indicator only in the open conversation UI (and optionally a subtle line in that room).
- Typing text overrides online/offline while active.
- Auto-clear typing UI ~3s after last `TypingStarted` if no refresh.

## Security

- Hub requires authenticated user.
- Join / typing only if user is participant of the conversation.
- Presence (online + lastSeen) is intentionally visible to other users (Messenger-like); do not expose IP, device, or connection ids.

## Error handling

- Hub connect failure: degrade to REST presence; no white screen.
- Automatic SignalR reconnect; resume heartbeat → online again.
- Invalid conversation join: ignore / return error without affecting other presence.

## Out of scope

- Redis / multi-instance SignalR backplane (single Docker API instance is enough for now).
- Read receipts beyond existing mark-read.
- Push notifications / mobile background presence.
- Showing “last seen” privacy toggles.

## Test plan

1. Two browsers/users: A online → B sees `Đang online`; A closes tab → within ~45s B sees `Hoạt động … trước`.
2. A types in conversation → B sees `Đang nhập...`; A stops → indicator clears.
3. Seller logged in with hub connected → buyer on product detail sees seller online.
4. Through Docker frontend `:3000`, WebSocket to `/hubs/presence` works.
5. Unauthenticated user does not connect hub; product page still loads (no status or anonymous-safe REST if exposed).

## Implementation notes (for plan)

- Backend: User field + migration, PresenceHub, presence service updating `LastSeenAt`, DTO/API extensions, Program.cs `MapHub`, JWT for SignalR.
- Frontend: `@microsoft/signalr`, hub hook, `PresenceLabel`, page wiring, nginx.conf hubs location.
- Align frontend `Conversation` type with backend DTO fields used by MessagesPage (`otherUserName` vs seller/buyer names).
