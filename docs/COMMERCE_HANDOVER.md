# PassDo Commerce Update Handover

## White-screen root cause (Edit product)
`EditProductPage` called `setSpeeds(...)` **during render**, causing a React crash (blank page).
Fixed by initializing speeds in `useEffect`, plus safer null coalescing, loading/error/retry states.

## Order statuses (no Shipper role)
```
AwaitingPayment → PendingConfirmation → AwaitingPreparation → AwaitingHandover → Shipping → Delivered
(+ Cancelled, DeliveryFailed, Returned, Refunded)
```

Courier info is entered by **seller** on hand-over (not a login role).

## Migrations
- `20260723174221_CommerceAndShipping`
- `20260724054020_RemoveShipperAddChatAndHandover`

## Key APIs
- `POST /api/orders/{id}/hand-over` — seller enters courier details → Shipping
- `POST /api/shipping/calculate` — backend fee rules (inner-city = 0)
- `POST/GET /api/conversations`, messages, mark-read
- `POST /api/auth/change-password`

## Shipping fee rules (Backend config `Shipping`)
- Same province + both districts in `InnerCity.Districts` → **0đ**
- Same province outer → `Fees.SameProvinceOuter`
- Nearby province pairs → `Fees.NearbyProvince`
- Else → `Fees.FarProvince`

## Seed accounts
- Admin: `admin@passdo.local` / `Admin@123456`
- Shipper account removed

## Frontend routes
- `/products/:id/edit` (fixed)
- `/messages`, `/messages/:id`
- Account dropdown → profile, settings tabs, purchases/sales, security, support
- Checkout uses `/api/shipping/calculate`

## Out of scope / gaps
- Real carrier/map distance APIs
- WebSocket realtime chat (polling 5s)
- Full return/refund workflow UI
- Session management beyond refresh tokens
