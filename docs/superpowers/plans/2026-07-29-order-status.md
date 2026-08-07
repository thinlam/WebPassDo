# PASSDO-04 Order Status Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename 3 OrderStatus members to match roadmap, add Completed terminal status, centralize transitions, add buyer confirm endpoint, update FE.

**Architecture:** Rename enum members (keeping int values unchanged); run data migration to update nvarchar strings in Orders + OrderStatusHistories; extract OrderStatusTransitions static helper; add CompleteOrderCommand + POST /api/orders/{id}/complete; update FE type union, labels, action buttons.

**Tech Stack:** ASP.NET Core / EF Core / MediatR / FluentValidation / xUnit+FluentAssertions+Moq; React + TypeScript + TanStack Query.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-29-order-status-design.md`
- Storage: `HasConversion<string>()` — rename requires SQL UPDATE migration
- Enum ints: `PendingSellerConfirmation=1, Preparing=2, ReadyForShipment=3, Completed=10`
- `Order.Status` default changes from `PendingConfirmation` to `PendingSellerConfirmation`
- Auto-complete (background job) is **out of scope** — design space only
- No Disputed status in this issue
- `OrderStatusGroups.ActiveProcessing` must include the renamed members (not old names)
- feat(PASSDO-04): … commit style; PowerShell-safe git commands

---

## File map

| File | Responsibility |
|------|----------------|
| `backend/src/PassDo.Domain/Enums/OrderStatus.cs` | Rename 3 + add Completed |
| `backend/src/PassDo.Domain/Entities/Order.cs` | Add `CompletedAt`; default status |
| `backend/src/PassDo.Application/Orders/OrderStatusTransitions.cs` | New static helper |
| `backend/src/PassDo.Application/Orders/DTOs/OrderDto.cs` | Update `OrderStatusGroups.ActiveProcessing` |
| `backend/src/PassDo.Application/Orders/Commands/OrderActions/OrderActionCommands.cs` | Add CompleteOrderCommand; update renamed references |
| `backend/src/PassDo.Api/Controllers/OrdersController.cs` | Add POST complete endpoint |
| `backend/src/PassDo.Infrastructure/Persistence/Migrations/*_RenameOrderStatuses.cs` | SQL rename data migration + CompletedAt column |
| `backend/tests/PassDo.UnitTests/Orders/OrderStatusTransitionTests.cs` | Helper + CompleteOrder tests |
| `frontend/src/types/index.ts` | OrderStatus union rename + Completed |
| `frontend/src/lib/orderStatus.ts` | Labels + tone for renamed + Completed |
| `frontend/src/pages/OrdersPages.tsx` | Tab keys/values + Completed tab |
| `frontend/src/pages/OrderDetailPage.tsx` | Rename status checks + buyer complete button |
| `frontend/src/features/orders/api.ts` | Add `complete()` |

---

### Task 1: Enum rename + migration + entity CompletedAt

**Files:**
- Modify: `backend/src/PassDo.Domain/Enums/OrderStatus.cs`
- Modify: `backend/src/PassDo.Domain/Entities/Order.cs`
- Create: EF migration `RenameOrderStatuses` via `dotnet ef migrations add`
- Modify: `backend/src/PassDo.Application/Orders/Commands/CreateOrder/CreateOrderCommand.cs` (default initial status)
- Modify: `backend/src/PassDo.Application/Orders/Commands/OrderActions/OrderActionCommands.cs` (fix renamed refs)
- Modify: `backend/src/PassDo.Application/Orders/DTOs/OrderDto.cs` (ActiveProcessing)
- Modify: `backend/src/PassDo.Infrastructure/Persistence/Migrations/*_RenameOrderStatuses.cs` (add SQL + CompletedAt column)

**Interfaces:**
- Produces: `OrderStatus.PendingSellerConfirmation`, `Preparing`, `ReadyForShipment`, `Completed`; `Order.CompletedAt`

- [ ] **Step 1: Rename enum + add Completed + CompletedAt**

`OrderStatus.cs`:

```csharp
namespace PassDo.Domain.Enums;

public enum OrderStatus
{
    AwaitingPayment = 0,
    PendingSellerConfirmation = 1,
    Preparing = 2,
    ReadyForShipment = 3,
    Shipping = 4,
    Delivered = 5,
    Cancelled = 6,
    DeliveryFailed = 7,
    Returned = 8,
    Refunded = 9,
    Completed = 10
}
```

`Order.cs` — add after `CancelledAt`:

```csharp
public DateTime? CompletedAt { get; set; }
```

Change default: `public OrderStatus Status { get; set; } = OrderStatus.PendingSellerConfirmation;`

- [ ] **Step 2: Fix all compile errors caused by rename (BE only)**

Search for `OrderStatus.PendingConfirmation`, `AwaitingPreparation`, `AwaitingHandover` in backend — replace with new names. Include `OrderStatusGroups.ActiveProcessing` in `OrderDto.cs`:

```csharp
public static readonly OrderStatus[] ActiveProcessing =
[
    OrderStatus.AwaitingPayment,
    OrderStatus.PendingSellerConfirmation,
    OrderStatus.Preparing,
    OrderStatus.ReadyForShipment,
    OrderStatus.Shipping
];
```

- [ ] **Step 3: Build backend to zero errors**

```bash
dotnet build backend/PassDo.sln
```

Expected: 0 errors.

- [ ] **Step 4: Generate + edit migration**

```bash
dotnet ef migrations add RenameOrderStatuses --project backend/src/PassDo.Infrastructure --startup-project backend/src/PassDo.Api --output-dir Persistence/Migrations
```

Edit the generated migration Up() to add:

```csharp
// Rename status strings in Orders table
migrationBuilder.Sql(@"
    UPDATE Orders SET [Status] = N'PendingSellerConfirmation' WHERE [Status] = N'PendingConfirmation';
    UPDATE Orders SET [Status] = N'Preparing'                 WHERE [Status] = N'AwaitingPreparation';
    UPDATE Orders SET [Status] = N'ReadyForShipment'          WHERE [Status] = N'AwaitingHandover';
");

// Rename in status history tables
migrationBuilder.Sql(@"
    UPDATE OrderStatusHistories SET [OldStatus] = N'PendingSellerConfirmation' WHERE [OldStatus] = N'PendingConfirmation';
    UPDATE OrderStatusHistories SET [OldStatus] = N'Preparing'                 WHERE [OldStatus] = N'AwaitingPreparation';
    UPDATE OrderStatusHistories SET [OldStatus] = N'ReadyForShipment'          WHERE [OldStatus] = N'AwaitingHandover';

    UPDATE OrderStatusHistories SET [NewStatus] = N'PendingSellerConfirmation' WHERE [NewStatus] = N'PendingConfirmation';
    UPDATE OrderStatusHistories SET [NewStatus] = N'Preparing'                 WHERE [NewStatus] = N'AwaitingPreparation';
    UPDATE OrderStatusHistories SET [NewStatus] = N'ReadyForShipment'          WHERE [NewStatus] = N'AwaitingHandover';
");
```

Down() reverses (old → new swap of strings).

The migration also creates the `CompletedAt` nullable DateTime column — verify the EF-generated column add is in Up() if EF detected the new property; otherwise add manually:

```csharp
migrationBuilder.AddColumn<DateTime>(
    name: "CompletedAt",
    table: "Orders",
    type: "datetime2",
    nullable: true);
```

- [ ] **Step 5: Run unit tests (should pass; no order tests need updating yet)**

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj -v q
```

Expected: PASS (existing tests should be green after compile fix).

- [ ] **Step 6: Commit**

```powershell
git add backend/src/PassDo.Domain backend/src/PassDo.Application backend/src/PassDo.Infrastructure
git commit -m "feat(PASSDO-04): rename order statuses and add Completed"
```

---

### Task 2: OrderStatusTransitions helper + CompleteOrderCommand + API endpoint

**Files:**
- Create: `backend/src/PassDo.Application/Orders/OrderStatusTransitions.cs`
- Modify: `backend/src/PassDo.Application/Orders/Commands/OrderActions/OrderActionCommands.cs`
- Modify: `backend/src/PassDo.Api/Controllers/OrdersController.cs`
- Test: `backend/tests/PassDo.UnitTests/Orders/OrderStatusTransitionTests.cs`

**Interfaces:**
- Consumes: `OrderStatus.*` renamed
- Produces:
  - `OrderStatusTransitions.IsTerminal(status) -> bool`
  - `OrderStatusTransitions.IsActive(status) -> bool`
  - `OrderStatusTransitions.CanBuyerConfirmComplete(status) -> bool`
  - `OrderStatusTransitions.IsProductReserving(status) -> bool`
  - `CompleteOrderCommand(Guid OrderId) : IRequest<OrderDetailDto>`

- [ ] **Step 1: Write failing tests**

Create `backend/tests/PassDo.UnitTests/Orders/OrderStatusTransitionTests.cs`:

```csharp
using FluentAssertions;
using PassDo.Application.Orders;
using PassDo.Domain.Enums;

namespace PassDo.UnitTests.Orders;

public class OrderStatusTransitionTests
{
    [Theory]
    [InlineData(OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.DeliveryFailed, true)]
    [InlineData(OrderStatus.Returned, true)]
    [InlineData(OrderStatus.Refunded, true)]
    [InlineData(OrderStatus.Completed, true)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Shipping, false)]
    [InlineData(OrderStatus.PendingSellerConfirmation, false)]
    public void IsTerminal_ReturnsExpected(OrderStatus s, bool expected)
        => OrderStatusTransitions.IsTerminal(s).Should().Be(expected);

    [Theory]
    [InlineData(OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Completed, false)]
    [InlineData(OrderStatus.Shipping, false)]
    public void CanBuyerConfirmComplete_ReturnsExpected(OrderStatus s, bool expected)
        => OrderStatusTransitions.CanBuyerConfirmComplete(s).Should().Be(expected);

    [Fact]
    public void IsActive_IsInverse_Of_IsTerminal()
    {
        foreach (OrderStatus s in Enum.GetValues<OrderStatus>())
            OrderStatusTransitions.IsActive(s).Should().Be(!OrderStatusTransitions.IsTerminal(s));
    }

    [Theory]
    [InlineData(OrderStatus.PendingSellerConfirmation, true)]
    [InlineData(OrderStatus.Preparing, true)]
    [InlineData(OrderStatus.ReadyForShipment, true)]
    [InlineData(OrderStatus.Shipping, true)]
    [InlineData(OrderStatus.AwaitingPayment, true)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Completed, false)]
    public void IsProductReserving_ReturnsExpected(OrderStatus s, bool expected)
        => OrderStatusTransitions.IsProductReserving(s).Should().Be(expected);
}
```

- [ ] **Step 2: Run — expect fail (type missing)**

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj --filter "FullyQualifiedName~OrderStatusTransitionTests" -v q
```

- [ ] **Step 3: Create OrderStatusTransitions**

`backend/src/PassDo.Application/Orders/OrderStatusTransitions.cs`:

```csharp
using PassDo.Domain.Enums;

namespace PassDo.Application.Orders;

public static class OrderStatusTransitions
{
    public static bool IsTerminal(OrderStatus s) =>
        s is OrderStatus.Cancelled
            or OrderStatus.DeliveryFailed
            or OrderStatus.Returned
            or OrderStatus.Refunded
            or OrderStatus.Completed;

    public static bool IsActive(OrderStatus s) => !IsTerminal(s);

    public static bool CanBuyerConfirmComplete(OrderStatus s) =>
        s == OrderStatus.Delivered;

    public static bool IsProductReserving(OrderStatus s) =>
        s is OrderStatus.AwaitingPayment
            or OrderStatus.PendingSellerConfirmation
            or OrderStatus.Preparing
            or OrderStatus.ReadyForShipment
            or OrderStatus.Shipping;
}
```

- [ ] **Step 4: Run helper tests — expect PASS**

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj --filter "FullyQualifiedName~OrderStatusTransitionTests" -v q
```

- [ ] **Step 5: Write failing CompleteOrder tests**

Add to a new class in `backend/tests/PassDo.UnitTests/Orders/OrderStatusTransitionTests.cs` or a separate `CompleteOrderTests.cs`. Use the same in-memory DB pattern from existing order tests:

```csharp
[Fact]
public async Task CompleteOrder_BuyerCanConfirm_WhenDelivered()
{
    // seed order Status=Delivered, buyer is current user
    // handle CompleteOrderCommand
    // assert Status == Completed, CompletedAt set
}

[Fact]
public async Task CompleteOrder_Idempotent_WhenAlreadyCompleted()
{
    // seed order Status=Completed
    // handle CompleteOrderCommand again
    // assert no exception; Status still Completed
}

[Fact]
public async Task CompleteOrder_Rejects_SellerCall()
{
    // seed order Status=Delivered, current user = seller (not buyer)
    // expect ForbiddenException
}

[Fact]
public async Task CompleteOrder_Rejects_NonDelivered()
{
    // seed order Status=Shipping
    // expect ConflictException
}
```

- [ ] **Step 6: Add CompleteOrderCommand to OrderActionCommands.cs**

Add at end of file before private helpers, or in the same class that already implements `IRequestHandler<...>`:

```csharp
public record CompleteOrderCommand(Guid OrderId) : IRequest<OrderDetailDto>;

// Implement handler as new class or alongside existing handler class
public class CompleteOrderCommandHandler : IRequestHandler<CompleteOrderCommand, OrderDetailDto>
{
    // inject same deps as OrderActionCommandHandler
    // Transition helper:
    //   if order.Status == Completed return loaded DTO (idempotent)
    //   if order.BuyerId != currentUser && !isAdmin -> ForbiddenException
    //   if !CanBuyerConfirmComplete(order.Status) -> ConflictException
    //   order.Status = Completed; order.CompletedAt = UtcNow
    //   ChangeStatus helper (Delivered -> Completed note)
    //   Notify seller
}
```

- [ ] **Step 7: Add controller endpoint**

In `OrdersController.cs`:

```csharp
[HttpPost("{id}/complete")]
public async Task<ActionResult<ApiResponse<object>>> Complete(Guid id)
{
    var result = await _mediator.Send(new CompleteOrderCommand(id));
    return Ok(ApiResponse<object>.Ok(result));
}
```

- [ ] **Step 8: Run full unit tests — expect PASS**

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj -v q
```

- [ ] **Step 9: Commit**

```powershell
git add backend/src/PassDo.Application/Orders backend/src/PassDo.Api backend/tests/PassDo.UnitTests/Orders
git commit -m "feat(PASSDO-04): add OrderStatusTransitions and CompleteOrder endpoint"
```

---

### Task 3: Frontend rename + labels + buyer complete button

**Files:**
- Modify: `frontend/src/types/index.ts`
- Modify: `frontend/src/lib/orderStatus.ts`
- Modify: `frontend/src/pages/OrdersPages.tsx`
- Modify: `frontend/src/pages/OrderDetailPage.tsx`
- Modify: `frontend/src/features/orders/api.ts`

**Interfaces:**
- Consumes: API status strings (PendingSellerConfirmation, Preparing, ReadyForShipment, Completed)
- Produces: Type-safe OrderStatus union; `ordersApi.complete(id)`; buyer confirm button

- [ ] **Step 1: Update types/index.ts**

```ts
export type OrderStatus =
  | 'AwaitingPayment'
  | 'PendingSellerConfirmation'
  | 'Preparing'
  | 'ReadyForShipment'
  | 'Shipping'
  | 'Delivered'
  | 'Completed'
  | 'Cancelled'
  | 'DeliveryFailed'
  | 'Returned'
  | 'Refunded'
```

Remove: `PendingConfirmation`, `AwaitingPreparation`, `AwaitingHandover`, `AwaitingPickup`.

- [ ] **Step 2: Update lib/orderStatus.ts labels + tone**

```ts
export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
  AwaitingPayment: 'Chờ thanh toán',
  PendingSellerConfirmation: 'Chờ xác nhận',
  Preparing: 'Đang chuẩn bị hàng',
  ReadyForShipment: 'Chờ bàn giao',
  Shipping: 'Đang giao hàng',
  Delivered: 'Đã giao',
  Completed: 'Hoàn tất',
  Cancelled: 'Đã hủy',
  DeliveryFailed: 'Giao thất bại',
  Returned: 'Trả hàng',
  Refunded: 'Hoàn tiền',
}
```

Update `getStatusTone`:
- `Completed`: return `'success'`
- Replace old names with new in switch cases

- [ ] **Step 3: Update OrdersPages.tsx tabs**

```ts
const STATUS_TABS = [
  { key: 'all', label: 'Tất cả' },
  { key: 'AwaitingPayment',            label: 'Chờ thanh toán',      value: 'AwaitingPayment' as OrderStatus },
  { key: 'PendingSellerConfirmation',  label: 'Chờ xác nhận',        value: 'PendingSellerConfirmation' as OrderStatus },
  { key: 'Preparing',                  label: 'Đang chuẩn bị hàng',  value: 'Preparing' as OrderStatus },
  { key: 'ReadyForShipment',           label: 'Chờ bàn giao',        value: 'ReadyForShipment' as OrderStatus },
  { key: 'Shipping',                   label: 'Đang giao',            value: 'Shipping' as OrderStatus },
  { key: 'Delivered',                  label: 'Đã giao',              value: 'Delivered' as OrderStatus },
  { key: 'Completed',                  label: 'Hoàn tất',             value: 'Completed' as OrderStatus },
  { key: 'Cancelled',                  label: 'Đã hủy',               value: 'Cancelled' as OrderStatus },
]
```

- [ ] **Step 4: Update OrderDetailPage.tsx**

Replace status string checks:
- `'PendingConfirmation'` → `'PendingSellerConfirmation'`
- `'AwaitingPreparation'` → `'Preparing'`
- `'AwaitingHandover'` → `'ReadyForShipment'`

Add buyer confirm-complete button (after existing buyer Shipping confirm block):

```tsx
{isBuyer && o.status === 'Delivered' && (
  <Button onClick={() => completeM.mutate()} disabled={completeM.isPending}>
    {completeM.isPending ? 'Đang xác nhận...' : 'Xác nhận đã nhận hàng'}
  </Button>
)}
```

Note: the existing `isBuyer && o.status === 'Shipping'` block calls `confirmDeliveredM` (not the new complete). Keep existing block; add the new one for `Delivered`.

Add `completeM` mutation alongside existing mutations:

```tsx
const completeM = useMutation({
  mutationFn: () => ordersApi.complete(o.id),
  onSuccess: () => queryClient.invalidateQueries({ queryKey: ['order', id] }),
  onError: (err) => setError(getErrorMessage(err)),
})
```

- [ ] **Step 5: Add `complete` to orders api.ts**

```ts
complete: (id: string) =>
  unwrap(apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/complete`)),
```

- [ ] **Step 6: Build FE — expect success**

```bash
cd frontend
npm run build
```

Fix any remaining old-name TypeScript errors.

- [ ] **Step 7: Grep cleanup**

```bash
rg "PendingConfirmation|AwaitingPreparation|AwaitingHandover|AwaitingPickup" frontend/src
```

Expected: no matches (old names gone from FE).

- [ ] **Step 8: Commit**

```powershell
git add frontend/src
git commit -m "feat(PASSDO-04): sync FE order status names and add complete action"
```

---

### Task 4: Docs + verify

**Files:**
- Modify: `docs/issues/PASSDO-04-order-status.md`
- Modify: `docs/issues/passdo-current-status.md`

- [ ] **Step 1: Update issue checklist**

Mark: enum renamed, data migration, Completed, transitions helper, buyer confirm, FE labels. Note deferred: auto-complete, Disputed, Refunded flow.

- [ ] **Step 2: Full verify**

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj -v q
cd frontend
npm run build
rg "PendingConfirmation|AwaitingPreparation|AwaitingHandover" backend/src frontend/src
```

Expected: tests pass; FE build pass; no stale old names in source (excluding migration files and OrderStatusHistories column names which refer to string data not code).

- [ ] **Step 3: Commit docs**

```powershell
git add docs/issues
git commit -m "docs(PASSDO-04): mark order status standardization done"
```

---

## Spec coverage checklist

| Spec requirement | Task |
|-----------------|------|
| Enum 3 renames + Completed=10 | 1 |
| Migration: UPDATE Orders + OrderStatusHistories | 1 |
| Order.CompletedAt column | 1 |
| OrderStatusGroups.ActiveProcessing updated | 1 |
| OrderStatusTransitions static helper | 2 |
| CompleteOrderCommand + idempotent | 2 |
| POST /api/orders/{id}/complete | 2 |
| FE type union + labels | 3 |
| Buyer confirm button + Completed tab | 3 |
| Tests: transitions, complete, idempotent, forbidden | 2 |
| Docs update | 4 |

## Self-review

- No placeholder steps; every code block uses the exact enum member names from the spec.
- `OrderStatusGroups.ActiveProcessing` updated in Task 1 Step 2 — used by PASSDO-03 one-order constraint.
- FE task removes `AwaitingPickup` from type union (stale alias from old codebase).
- Migration Down() required for reversibility — include reversal SQL in Down().
