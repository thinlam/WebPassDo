# PASSDO-06 — Reject Reason Standardization Design

Date: 2026-08-13
Status: Approved in conversation (awaiting file review)
Scope: Standardize seller reject reason (enum + note), OrderRejected notification, tests. Chosen approach A (enum + optional note, no schema migration).

## Goal

Replace free-text `Reason` on `RejectOrderCommand` with a structured `OrderRejectReason` enum + optional note, add a dedicated `OrderRejected` notification type, and cover with unit tests.

## Decisions

| Topic | Choice |
|-------|--------|
| Storage | No new column. `Order.CancellationReason` (existing `nvarchar`) stores the formatted label / `"Khác: {note}"` string |
| Enum | New `OrderRejectReason` in `PassDo.Domain.Enums` |
| API contract | `RejectOrderCommand(Guid OrderId, OrderRejectReason ReasonCode, string? ReasonNote)` |
| Validation | `ReasonNote` required + max 500 chars when `ReasonCode == Other`; optional otherwise (max 500 if provided) |
| Notification | New `NotificationTypes.OrderRejected` used only for seller reject; buyer/seller `Cancel` keeps `OrderCancelled` |
| Backward compat | This is an internal FE+BE pair (no external clients); breaking the reject request shape is acceptable. `CancelOrderCommand` and other actions are untouched |
| Out of scope | Renaming `mark-prepared`/`confirm` endpoints; unique-active-order constraint (done in PASSDO-03); reject reasons for `CancelOrderCommand` (buyer-initiated, stays free text) |

## Enum

```csharp
namespace PassDo.Domain.Enums;

public enum OrderRejectReason
{
    OutOfStock = 0,      // Hết hàng
    SoldElsewhere = 1,   // Đã bán nơi khác
    CannotDeliver = 2,   // Không giao được
    WrongPrice = 3,      // Sai giá
    Other = 4            // Khác
}
```

VN labels (shared BE + FE mapping):

| Code | Label |
|------|-------|
| OutOfStock | Hết hàng |
| SoldElsewhere | Đã bán nơi khác |
| CannotDeliver | Không giao được |
| WrongPrice | Sai giá |
| Other | Khác |

## Backend changes

### `RejectOrderCommand` (OrderActionCommands.cs)

```csharp
public record RejectOrderCommand(Guid OrderId, OrderRejectReason ReasonCode, string? ReasonNote) : IRequest<OrderDetailDto>;

public class RejectOrderCommandValidator : AbstractValidator<RejectOrderCommand>
{
    public RejectOrderCommandValidator()
    {
        RuleFor(x => x.ReasonCode).IsInEnum();
        RuleFor(x => x.ReasonNote).MaximumLength(500);
        RuleFor(x => x.ReasonNote)
            .Must(note => !string.IsNullOrWhiteSpace(note))
            .WithMessage("Vui lòng nhập lý do khi chọn 'Khác'.")
            .When(x => x.ReasonCode == OrderRejectReason.Other);
    }
}
```

Handler builds the display string:

```csharp
private static string FormatRejectReason(OrderRejectReason code, string? note)
{
    var label = OrderRejectReasonLabels.Get(code); // "Hết hàng", "Khác", ...
    if (code == OrderRejectReason.Other)
        return $"Khác: {note}";
    return string.IsNullOrWhiteSpace(note) ? label : $"{label} — {note}";
}
```

`Handle(RejectOrderCommand)`:
- Keep existing ownership/status checks (`EnsureSellerOrAdmin`, status must be `PendingSellerConfirmation` or `AwaitingPayment`)
- `RestoreStock`, set `CancelledAt`, `CancellationReason = FormatRejectReason(...)`
- `ChangeStatus(order, OrderStatus.Cancelled, formattedReason)`
- `afterSave`: `NotifyBuyer(order, NotificationTypes.OrderRejected, "Đơn hàng bị từ chối", $"Người bán đã từ chối đơn hàng {order.OrderCode} - \"{ProductName(order)}\". Lý do: {formattedReason}", ct)`

### `OrderRejectReasonLabels` helper

New small static class (`PassDo.Application.Orders` namespace) mapping enum → VN label, reused by handler; no need to expose via API (FE has its own copy for the select, matching values by enum name).

### `NotificationTypes.cs`

Add:
```csharp
public const string OrderRejected = "OrderRejected";
```

### Controller (`OrdersController.cs`)

`Reject` action body becomes `RejectOrderRequest { ReasonCode, ReasonNote }` (contract in `PassDo.Api.Contracts.Orders`), mapped to `RejectOrderCommand`.

## Frontend changes

| File | Change |
|------|--------|
| `frontend/src/types/index.ts` | Add `OrderRejectReason` union: `'OutOfStock' | 'SoldElsewhere' | 'CannotDeliver' | 'WrongPrice' | 'Other'` |
| `frontend/src/lib/orderStatus.ts` | Add `ORDER_REJECT_REASON_LABELS` map (VN labels matching backend) |
| `frontend/src/features/orders/api.ts` | Change `reject(id, reason: string)` → `reject(id, payload: { reasonCode: OrderRejectReason; reasonNote?: string })` |
| `frontend/src/pages/OrderDetailPage.tsx` | Replace free-text reject flow with: a `<Select>` of the 5 reasons + conditional note `<Input>` (required when `Other`); disable submit until valid |

No new modal component required — reuse the existing action panel area (below existing `actionNote` input, or a small inline reject panel that only appears when reject button area is active). Simplest implementation: local state `rejectReasonCode` + reuse `actionNote` as `reasonNote` for the reject action specifically.

## Tests

`backend/tests/PassDo.UnitTests/Orders/` (extend `OrderOwnershipTests.cs` or new `RejectOrderCommandTests.cs`):

- Reject with each of the 5 `ReasonCode` values (non-Other) succeeds, `CancellationReason` contains the VN label
- Reject with `Other` and no note → validation failure
- Reject with `Other` and a note → `CancellationReason == "Khác: {note}"`
- Reject sends `NotificationTypes.OrderRejected` (not `OrderCancelled`)
- Buyer-initiated `CancelOrderCommand` still sends `NotificationTypes.OrderCancelled` (regression check)
- Non-owner/non-admin reject still throws `ForbiddenException` (existing behavior, keep passing)

## Definition of Done

- [ ] `OrderRejectReason` enum + labels helper
- [ ] `RejectOrderCommand` takes `ReasonCode` + `ReasonNote`; validator enforces `Other` note
- [ ] `NotificationTypes.OrderRejected` used for seller reject
- [ ] Controller + request contract updated
- [ ] FE: reject select + conditional note + api.ts updated
- [ ] Unit tests pass (new + regression)
- [ ] `docs/issues/PASSDO-06-seller-confirm.md` checklist updated
