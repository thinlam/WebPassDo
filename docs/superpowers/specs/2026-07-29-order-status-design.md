# PASSDO-04 — Order Status Standardization Design

Date: 2026-07-29  
Status: Approved in conversation (awaiting file review)  
Scope: Rename 6 enum members to match roadmap, add Completed, OrderStatusTransitions helper, buyer confirm, FE labels

## Goal

Align order status enum with roadmap naming, add `Completed` terminal status, centralize transition rules, and expose a buyer-confirm endpoint. Auto-complete background job is deferred.

## Decisions

| Topic | Choice |
|-------|--------|
| Storage | `nvarchar(50)` via `HasConversion<string>()` — rename requires data migration |
| Rename 3 members | PendingConfirmation → PendingSellerConfirmation; AwaitingPreparation → Preparing; AwaitingHandover → ReadyForShipment |
| Keep 7 members | AwaitingPayment, Shipping, Delivered, Cancelled, DeliveryFailed, Returned, Refunded |
| Add | Completed = 10 (terminal) |
| CompletedAt | Add nullable `DateTime? CompletedAt` on `Order` entity |
| Buyer confirm | `POST /api/orders/{id}/complete` — buyer or admin only; Delivered required; idempotent |
| Auto-complete | Deferred — design reserves space (CompletedAt, IsTerminal) |
| Transitions helper | `OrderStatusTransitions.cs` (static, similar to ProductStatusTransitions) |
| State machine in handlers | Update existing if/switch to use helpers; do not rewrite as full IStateMachine |
| FE | Update type union, labels, status-filter tabs, buyer confirm button on OrderDetailPage |

## Enum

```csharp
public enum OrderStatus
{
    AwaitingPayment = 0,
    PendingSellerConfirmation = 1,   // was PendingConfirmation
    Preparing = 2,                   // was AwaitingPreparation
    ReadyForShipment = 3,            // was AwaitingHandover
    Shipping = 4,
    Delivered = 5,
    Cancelled = 6,
    DeliveryFailed = 7,
    Returned = 8,
    Refunded = 9,
    Completed = 10
}
```

Numeric values: unchanged for kept members; Completed appends at 10.

## Data migration

SQL in EF migration Up():

```sql
UPDATE Orders         SET [Status]    = N'PendingSellerConfirmation' WHERE [Status]    = N'PendingConfirmation';
UPDATE Orders         SET [Status]    = N'Preparing'                 WHERE [Status]    = N'AwaitingPreparation';
UPDATE Orders         SET [Status]    = N'ReadyForShipment'          WHERE [Status]    = N'AwaitingHandover';

UPDATE OrderStatusHistories SET [OldStatus] = N'PendingSellerConfirmation' WHERE [OldStatus] = N'PendingConfirmation';
UPDATE OrderStatusHistories SET [OldStatus] = N'Preparing'                 WHERE [OldStatus] = N'AwaitingPreparation';
UPDATE OrderStatusHistories SET [OldStatus] = N'ReadyForShipment'          WHERE [OldStatus] = N'AwaitingHandover';

UPDATE OrderStatusHistories SET [NewStatus] = N'PendingSellerConfirmation' WHERE [NewStatus] = N'PendingConfirmation';
UPDATE OrderStatusHistories SET [NewStatus] = N'Preparing'                 WHERE [NewStatus] = N'AwaitingPreparation';
UPDATE OrderStatusHistories SET [NewStatus] = N'ReadyForShipment'          WHERE [NewStatus] = N'AwaitingHandover';
```

Down() reverses. No migration of Delivered → Completed for existing rows.

## OrderStatusTransitions helper

```csharp
// PassDo.Application.Orders.OrderStatusTransitions
public static class OrderStatusTransitions
{
    public static bool IsTerminal(OrderStatus s) =>
        s is Cancelled or DeliveryFailed or Returned or Refunded or Completed;

    public static bool IsActive(OrderStatus s) => !IsTerminal(s);

    public static bool CanBuyerConfirmComplete(OrderStatus s) => s == Delivered;

    // Returns true if the status is one that an active reservation holds on the product.
    // Used by PASSDO-03 product status logic; Completed does NOT hold a reservation.
    public static bool IsProductReserving(OrderStatus s) =>
        s is PendingSellerConfirmation or Preparing or ReadyForShipment or Shipping or AwaitingPayment;
}
```

`OrderStatusGroups.ActiveProcessing` in `OrderDto.cs` updated to use the three renamed members.

## CompleteOrderCommand

New command in `OrderActionCommands.cs` (or separate file in same folder):

- Actor: buyer of the order, or Admin
- Precondition: `order.Status == Delivered`
- Idempotent: if already `Completed`, return DTO without throwing
- Sets `order.Status = Completed`, `order.CompletedAt = UtcNow`
- Appends status history `Delivered → Completed`
- Sends notification to Seller: "Buyer đã xác nhận hoàn tất đơn {code}"

API endpoint: `POST /api/orders/{id}/complete` (no body required)

## Order entity changes

Add to `Order`:

```csharp
public DateTime? CompletedAt { get; set; }
```

No migration required for column if added via EF (new nullable column).

## Frontend

Files to update:

| File | Change |
|------|--------|
| `frontend/src/types/index.ts` | Rename 3 members in `OrderStatus` union; add `Completed` |
| `frontend/src/lib/orderStatus.ts` | Update `ORDER_STATUS_LABELS`, `getStatusTone`; add `Completed: 'Hoàn tất'` (tone: success) |
| `frontend/src/pages/OrdersPages.tsx` | Rename filter tab labels/keys; add Completed tab |
| `frontend/src/pages/OrderDetailPage.tsx` | Replace old names in status checks; add buyer "Xác nhận đã nhận hàng" button for Delivered state |
| `frontend/src/features/orders/api.ts` | Add `complete: (id) => unwrap(apiClient.post(...))` |

Status checks rename: `PendingConfirmation → PendingSellerConfirmation`, `AwaitingPreparation → Preparing`, `AwaitingHandover → ReadyForShipment`.

## Tests required

- Rename: API returns new string names; old names rejected by enum binding
- Completed command: buyer confirm Delivered → Completed; admin can do same; seller rejected; non-Delivered rejected; idempotent (double call)
- OrderStatusTransitions helpers: IsTerminal, IsActive, CanBuyerConfirmComplete
- FE: type compiles with new names

## Out of scope (follow-up)

- Auto-complete BackgroundService (N-day timer after DeliveredAt)
- Disputed status (PASSDO-18)
- Refunded flow (no trigger today)
- DeliveryFailed / Returned rename (not in roadmap mapping)

## Definition of Done

- [ ] Enum has all 11 members with correct names
- [ ] DB strings migrated (no stale PendingConfirmation/AwaitingPreparation/AwaitingHandover)
- [ ] CompleteOrder API works; buyer confirm only; idempotent
- [ ] OrderStatusTransitions helper + OrderStatusGroups updated
- [ ] FE type + labels + buyer confirm button
- [ ] Tests pass
- [ ] PASSDO-04 issue doc updated
