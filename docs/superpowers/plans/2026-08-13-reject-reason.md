# PASSDO-06 Reject Reason Standardization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace free-text seller reject reason with a structured `OrderRejectReason` enum + optional note, add a dedicated `OrderRejected` notification, and update the frontend reject UI.

**Architecture:** Add `OrderRejectReason` enum + label helper in the domain/application layer; change `RejectOrderCommand` shape and its validator; update the controller contract; add a new notification constant; update FE types/api/UI to send structured payload with a select + conditional note.

**Tech Stack:** ASP.NET Core / EF Core / MediatR / FluentValidation / xUnit+FluentAssertions+Moq; React + TypeScript + TanStack Query.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-08-13-reject-reason-design.md`
- No new DB column — `Order.CancellationReason` (existing `nvarchar`) stores the formatted string
- `ReasonNote` required (non-whitespace) when `ReasonCode == Other`, max 500 chars always
- `NotificationTypes.OrderRejected` used ONLY for seller reject; `CancelOrderCommand` (buyer-initiated) and other cancel-like paths keep `NotificationTypes.OrderCancelled` — do not change those
- Breaking the `/orders/{id}/reject` request shape is acceptable (no external clients)
- feat(PASSDO-06) commit style; PowerShell-safe git commands (`;` not `&&` if running directly, though subagents may use bash-style — confirm shell first)

---

## File map

| File | Responsibility |
|------|-----------------|
| `backend/src/PassDo.Domain/Enums/OrderRejectReason.cs` | New enum: OutOfStock, SoldElsewhere, CannotDeliver, WrongPrice, Other |
| `backend/src/PassDo.Application/Orders/OrderRejectReasonLabels.cs` | New static VN label lookup + formatter |
| `backend/src/PassDo.Domain/Constants/NotificationTypes.cs` | Add `OrderRejected` constant |
| `backend/src/PassDo.Application/Orders/Commands/OrderActions/OrderActionCommands.cs` | Change `RejectOrderCommand` record + validator + handler body + notification type |
| `backend/src/PassDo.Api/Contracts/Orders/OrderRequests.cs` | Add `RejectOrderRequest(OrderRejectReason ReasonCode, string? ReasonNote)` |
| `backend/src/PassDo.Api/Controllers/OrdersController.cs` | Update `Reject` action to use new request/command shape |
| `backend/tests/PassDo.UnitTests/Orders/RejectOrderCommandTests.cs` | New test file covering all reject scenarios |
| `frontend/src/types/index.ts` | Add `OrderRejectReason` union type |
| `frontend/src/lib/orderStatus.ts` | Add `ORDER_REJECT_REASON_LABELS` map |
| `frontend/src/features/orders/api.ts` | Change `reject()` signature to structured payload |
| `frontend/src/pages/OrderDetailPage.tsx` | Replace free-text reject with reason select + conditional note |

---

### Task 1: Backend enum, labels, notification constant

**Files:**
- Create: `backend/src/PassDo.Domain/Enums/OrderRejectReason.cs`
- Create: `backend/src/PassDo.Application/Orders/OrderRejectReasonLabels.cs`
- Modify: `backend/src/PassDo.Domain/Constants/NotificationTypes.cs`

**Interfaces:**
- Produces: `OrderRejectReason` enum (5 values); `OrderRejectReasonLabels.Format(OrderRejectReason code, string? note) -> string`; `NotificationTypes.OrderRejected`

- [ ] **Step 1: Create the enum**

`backend/src/PassDo.Domain/Enums/OrderRejectReason.cs`:

```csharp
namespace PassDo.Domain.Enums;

public enum OrderRejectReason
{
    OutOfStock = 0,
    SoldElsewhere = 1,
    CannotDeliver = 2,
    WrongPrice = 3,
    Other = 4
}
```

- [ ] **Step 2: Create the label/formatter helper**

`backend/src/PassDo.Application/Orders/OrderRejectReasonLabels.cs`:

```csharp
using PassDo.Domain.Enums;

namespace PassDo.Application.Orders;

public static class OrderRejectReasonLabels
{
    private static readonly Dictionary<OrderRejectReason, string> Labels = new()
    {
        [OrderRejectReason.OutOfStock] = "Hết hàng",
        [OrderRejectReason.SoldElsewhere] = "Đã bán nơi khác",
        [OrderRejectReason.CannotDeliver] = "Không giao được",
        [OrderRejectReason.WrongPrice] = "Sai giá",
        [OrderRejectReason.Other] = "Khác"
    };

    public static string Get(OrderRejectReason code) => Labels[code];

    public static string Format(OrderRejectReason code, string? note)
    {
        if (code == OrderRejectReason.Other)
        {
            return $"Khác: {note}";
        }

        var label = Get(code);
        return string.IsNullOrWhiteSpace(note) ? label : $"{label} — {note}";
    }
}
```

- [ ] **Step 3: Add notification constant**

In `backend/src/PassDo.Domain/Constants/NotificationTypes.cs`, add after `OrderCancelled`:

```csharp
    public const string OrderCancelled = "OrderCancelled";
    public const string OrderRejected = "OrderRejected";
```

- [ ] **Step 4: Build backend to verify no errors**

Run: `dotnet build backend/PassDo.sln`
Expected: 0 errors (new files compile standalone; nothing references them yet).

- [ ] **Step 5: Commit**

```bash
git add backend/src/PassDo.Domain/Enums/OrderRejectReason.cs backend/src/PassDo.Application/Orders/OrderRejectReasonLabels.cs backend/src/PassDo.Domain/Constants/NotificationTypes.cs
git commit -m "feat(PASSDO-06): add OrderRejectReason enum and label formatter"
```

---

### Task 2: RejectOrderCommand + validator + handler + controller contract

**Files:**
- Modify: `backend/src/PassDo.Application/Orders/Commands/OrderActions/OrderActionCommands.cs`
- Modify: `backend/src/PassDo.Api/Contracts/Orders/OrderRequests.cs`
- Modify: `backend/src/PassDo.Api/Controllers/OrdersController.cs`
- Test: `backend/tests/PassDo.UnitTests/Orders/RejectOrderCommandTests.cs`

**Interfaces:**
- Consumes: `OrderRejectReason`, `OrderRejectReasonLabels.Format`, `NotificationTypes.OrderRejected` (Task 1)
- Produces: `RejectOrderCommand(Guid OrderId, OrderRejectReason ReasonCode, string? ReasonNote)`; `RejectOrderRequest(OrderRejectReason ReasonCode, string? ReasonNote)`

- [ ] **Step 1: Write the failing tests**

Create `backend/tests/PassDo.UnitTests/Orders/RejectOrderCommandTests.cs`. This mirrors the `CreateSut`/`SeedOrder` pattern from `OrderOwnershipTests.cs` (same file, same namespace `PassDo.UnitTests.Orders`) — duplicate the two private helpers into this file (do not modify `OrderOwnershipTests.cs`):

```csharp
using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Moq;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Options;
using PassDo.Application.Orders.Commands.OrderActions;
using PassDo.Domain.Constants;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;
using PassDo.Infrastructure.Persistence;

namespace PassDo.UnitTests.Orders;

public class RejectOrderCommandTests
{
    private static (PassDoDbContext Db, Mock<ICurrentUserService> CurrentUser, Mock<INotificationService> Notifications, OrderActionHandler Handler) CreateSut(Guid actorId)
    {
        var options = new DbContextOptionsBuilder<PassDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(actorId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Role).Returns("User");

        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        var db = new PassDoDbContext(options, currentUser.Object, dateTime.Object);
        var shipping = new Mock<IShippingCalculator>();
        var notifications = new Mock<INotificationService>();

        var handler = new OrderActionHandler(
            db,
            currentUser.Object,
            shipping.Object,
            dateTime.Object,
            notifications.Object);

        return (db, currentUser, notifications, handler);
    }

    private static Order SeedOrder(
        PassDoDbContext db,
        Guid buyerId,
        Guid sellerId,
        OrderStatus status = OrderStatus.PendingSellerConfirmation)
    {
        var productId = Guid.NewGuid();
        db.Users.AddRange(
            new User { Id = buyerId, Email = "buyer@test.com", FullName = "Buyer", PasswordHash = "x", CreatedAt = DateTime.UtcNow },
            new User { Id = sellerId, Email = "seller@test.com", FullName = "Seller", PasswordHash = "x", CreatedAt = DateTime.UtcNow });

        db.Products.Add(new Product
        {
            Id = productId,
            Name = "Item",
            Description = "desc",
            SellingPrice = 100,
            OriginalPrice = 200,
            Condition = ProductCondition.Used,
            Status = ProductStatus.Reserved,
            Location = "HCM",
            Quantity = 0,
            CategoryId = Guid.NewGuid(),
            SellerId = sellerId,
            CreatedAt = DateTime.UtcNow
        });

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = "PD-TEST-002",
            ProductId = productId,
            BuyerId = buyerId,
            SellerId = sellerId,
            ProductTotal = 100,
            ShippingFee = 0,
            GrandTotal = 100,
            Price = 100,
            Status = status,
            PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Unpaid,
            DeliverySpeed = DeliverySpeed.Standard,
            ShippingRecipientName = "Buyer",
            ShippingPhone = "0900000000",
            ShippingProvince = "HCM",
            ShippingDistrict = "Q1",
            ShippingWard = "P1",
            ShippingStreetAddress = "1 Nguyen Hue",
            PickupRecipientName = "Seller",
            PickupPhone = "0900000001",
            PickupProvince = "HCM",
            PickupDistrict = "Q3",
            PickupWard = "P2",
            PickupStreetAddress = "2 Le Loi",
            CreatedAt = DateTime.UtcNow,
            Items =
            {
                new OrderItem { ProductId = productId, ProductName = "Item", UnitPrice = 100, Quantity = 1, LineTotal = 100 }
            }
        };

        db.Orders.Add(order);
        db.SaveChanges();
        return order;
    }

    [Theory]
    [InlineData(OrderRejectReason.OutOfStock, "Hết hàng")]
    [InlineData(OrderRejectReason.SoldElsewhere, "Đã bán nơi khác")]
    [InlineData(OrderRejectReason.CannotDeliver, "Không giao được")]
    [InlineData(OrderRejectReason.WrongPrice, "Sai giá")]
    public async Task RejectOrder_NonOtherReason_SetsCancellationReasonToLabel(OrderRejectReason code, string expectedLabel)
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var (db, _, notifications, handler) = CreateSut(sellerId);
        var order = SeedOrder(db, buyerId, sellerId);

        var result = await handler.Handle(new RejectOrderCommand(order.Id, code, null), CancellationToken.None);

        result.CancellationReason.Should().Be(expectedLabel);
        result.Status.Should().Be("Cancelled");
        notifications.Verify(x => x.NotifyAsync(
            buyerId,
            NotificationTypes.OrderRejected,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectOrder_OtherReasonWithNote_FormatsAsKhacPrefix()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var (db, _, _, handler) = CreateSut(sellerId);
        var order = SeedOrder(db, buyerId, sellerId);

        var result = await handler.Handle(
            new RejectOrderCommand(order.Id, OrderRejectReason.Other, "Đổi ý không bán nữa"),
            CancellationToken.None);

        result.CancellationReason.Should().Be("Khác: Đổi ý không bán nữa");
    }

    [Fact]
    public void Validator_OtherReasonWithoutNote_Fails()
    {
        var validator = new RejectOrderCommandValidator();
        var result = validator.TestValidate(new RejectOrderCommand(Guid.NewGuid(), OrderRejectReason.Other, null));

        result.ShouldHaveValidationErrorFor(x => x.ReasonNote);
    }

    [Fact]
    public void Validator_OtherReasonWithWhitespaceNote_Fails()
    {
        var validator = new RejectOrderCommandValidator();
        var result = validator.TestValidate(new RejectOrderCommand(Guid.NewGuid(), OrderRejectReason.Other, "   "));

        result.ShouldHaveValidationErrorFor(x => x.ReasonNote);
    }

    [Fact]
    public void Validator_NonOtherReasonWithoutNote_Passes()
    {
        var validator = new RejectOrderCommandValidator();
        var result = validator.TestValidate(new RejectOrderCommand(Guid.NewGuid(), OrderRejectReason.OutOfStock, null));

        result.ShouldNotHaveValidationErrorFor(x => x.ReasonNote);
    }

    [Fact]
    public void Validator_NoteExceeding500Chars_Fails()
    {
        var validator = new RejectOrderCommandValidator();
        var longNote = new string('a', 501);
        var result = validator.TestValidate(new RejectOrderCommand(Guid.NewGuid(), OrderRejectReason.OutOfStock, longNote));

        result.ShouldHaveValidationErrorFor(x => x.ReasonNote);
    }

    [Fact]
    public async Task CancelOrder_ByBuyer_StillUsesOrderCancelledNotification()
    {
        var buyerId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var (db, _, notifications, handler) = CreateSut(buyerId);
        var order = SeedOrder(db, buyerId, sellerId, OrderStatus.AwaitingPayment);

        await handler.Handle(new CancelOrderCommand(order.Id, "Đổi ý"), CancellationToken.None);

        notifications.Verify(x => x.NotifyAsync(
            sellerId,
            NotificationTypes.OrderCancelled,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

Note: check the exact `INotificationService.NotifyAsync` signature in `backend/src/PassDo.Application/Common/Interfaces/INotificationService.cs` before running — if the parameter count/order differs, adjust the `Verify(...)` call signature to match exactly (this is the one place in the plan where you must cross-check an existing interface rather than guess).

- [ ] **Step 2: Run tests to verify they fail (compile errors expected — RejectOrderCommand shape not changed yet)**

Run: `dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj --filter "FullyQualifiedName~RejectOrderCommandTests" -v q`
Expected: FAIL to build (RejectOrderCommand still takes `string Reason`, not `OrderRejectReason ReasonCode`).

- [ ] **Step 3: Update RejectOrderCommand record and validator**

In `backend/src/PassDo.Application/Orders/Commands/OrderActions/OrderActionCommands.cs`, find:

```csharp
public record RejectOrderCommand(Guid OrderId, string Reason) : IRequest<OrderDetailDto>;
```

Replace with:

```csharp
public record RejectOrderCommand(Guid OrderId, OrderRejectReason ReasonCode, string? ReasonNote) : IRequest<OrderDetailDto>;
```

Find:

```csharp
public class RejectOrderCommandValidator : AbstractValidator<RejectOrderCommand>
{
    public RejectOrderCommandValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
}
```

Replace with:

```csharp
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

Add `using PassDo.Domain.Enums;` at the top of the file if not already present (it already imports `PassDo.Domain.Enums` — verify before adding a duplicate).

- [ ] **Step 4: Update the RejectOrderCommand handler**

Find the `Handle(RejectOrderCommand request, ...)` method:

```csharp
public Task<OrderDetailDto> Handle(RejectOrderCommand request, CancellationToken ct)
    => Transition(request.OrderId, async order =>
    {
        EnsureSellerOrAdmin(order);
        if (order.Status is not (OrderStatus.PendingSellerConfirmation or OrderStatus.AwaitingPayment))
        {
            throw new ConflictException("Order cannot be rejected in the current status.");
        }

        await RestoreStock(order, ct);
        order.CancelledAt = _clock.UtcNow;
        order.CancellationReason = request.Reason;
        ChangeStatus(order, OrderStatus.Cancelled, request.Reason);
    }, ct, afterSave: order => NotifyBuyer(
        order,
        NotificationTypes.OrderCancelled,
        "Đơn hàng đã bị hủy",
        $"Người bán đã từ chối đơn hàng {order.OrderCode} - \"{ProductName(order)}\". Lý do: {request.Reason}",
        ct));
```

Replace with:

```csharp
public Task<OrderDetailDto> Handle(RejectOrderCommand request, CancellationToken ct)
{
    var formattedReason = OrderRejectReasonLabels.Format(request.ReasonCode, request.ReasonNote);
    return Transition(request.OrderId, async order =>
    {
        EnsureSellerOrAdmin(order);
        if (order.Status is not (OrderStatus.PendingSellerConfirmation or OrderStatus.AwaitingPayment))
        {
            throw new ConflictException("Order cannot be rejected in the current status.");
        }

        await RestoreStock(order, ct);
        order.CancelledAt = _clock.UtcNow;
        order.CancellationReason = formattedReason;
        ChangeStatus(order, OrderStatus.Cancelled, formattedReason);
    }, ct, afterSave: order => NotifyBuyer(
        order,
        NotificationTypes.OrderRejected,
        "Đơn hàng bị từ chối",
        $"Người bán đã từ chối đơn hàng {order.OrderCode} - \"{ProductName(order)}\". Lý do: {formattedReason}",
        ct));
}
```

Add `using PassDo.Application.Orders;` if `OrderRejectReasonLabels` is not already in scope (it's in the same namespace `PassDo.Application.Orders` as this file's `namespace PassDo.Application.Orders.Commands.OrderActions;` parent — check whether an explicit using is needed; the sibling namespace requires `using PassDo.Application.Orders;`).

- [ ] **Step 5: Update the API contract**

In `backend/src/PassDo.Api/Contracts/Orders/OrderRequests.cs`, find:

```csharp
public record ReasonRequest(string Reason);
```

Leave `ReasonRequest` as-is (still used by `Cancel` and `FailDelivery`). Add a new record right after it:

```csharp
public record RejectOrderRequest(OrderRejectReason ReasonCode, string? ReasonNote);
```

- [ ] **Step 6: Update the controller**

In `backend/src/PassDo.Api/Controllers/OrdersController.cs`, find:

```csharp
[HttpPost("{id:guid}/reject")]
public async Task<ActionResult<ApiResponse<object>>> Reject(Guid id, [FromBody] ReasonRequest request)
{
    var result = await _mediator.Send(new RejectOrderCommand(id, request.Reason));
    return Ok(ApiResponse<object>.Ok(result, "Order rejected."));
}
```

Replace with:

```csharp
[HttpPost("{id:guid}/reject")]
public async Task<ActionResult<ApiResponse<object>>> Reject(Guid id, [FromBody] RejectOrderRequest request)
{
    var result = await _mediator.Send(new RejectOrderCommand(id, request.ReasonCode, request.ReasonNote));
    return Ok(ApiResponse<object>.Ok(result, "Order rejected."));
}
```

- [ ] **Step 7: Build and run the new tests**

Run: `dotnet build backend/PassDo.sln`
Expected: 0 errors.

Run: `dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj --filter "FullyQualifiedName~RejectOrderCommandTests" -v q`
Expected: PASS (all 8 tests).

If `INotificationService.NotifyAsync` verify calls fail to compile due to signature mismatch, open `backend/src/PassDo.Application/Common/Interfaces/INotificationService.cs`, copy the exact method signature, and fix the `Verify(...)` argument list/order in the test file to match — do not change the interface.

- [ ] **Step 8: Run the full unit test suite to check for regressions**

Run: `dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj -v q`
Expected: all tests pass (including previously-passing `OrderOwnershipTests`, `CompleteOrderCommandTests`, etc.) — no other file references the old `RejectOrderCommand(Guid, string)` shape except the ones changed in this task; if the build reports other call sites, fix them (search: `RejectOrderCommand(` in the whole `backend/` tree).

- [ ] **Step 9: Commit**

```bash
git add backend/src/PassDo.Application/Orders/Commands/OrderActions/OrderActionCommands.cs backend/src/PassDo.Api/Contracts/Orders/OrderRequests.cs backend/src/PassDo.Api/Controllers/OrdersController.cs backend/tests/PassDo.UnitTests/Orders/RejectOrderCommandTests.cs
git commit -m "feat(PASSDO-06): structure reject reason as enum + note, add OrderRejected notification"
```

---

### Task 3: Frontend reject reason select + conditional note

**Files:**
- Modify: `frontend/src/types/index.ts`
- Modify: `frontend/src/lib/orderStatus.ts`
- Modify: `frontend/src/features/orders/api.ts`
- Modify: `frontend/src/pages/OrderDetailPage.tsx`

**Interfaces:**
- Consumes: backend reject endpoint now expects `{ reasonCode, reasonNote }` (Task 2)
- Produces: `OrderRejectReason` type; `ORDER_REJECT_REASON_LABELS`; `ordersApi.reject(id, payload)`

- [ ] **Step 1: Add the type**

In `frontend/src/types/index.ts`, find the `OrderStatus` type block (around line 53) and add right after its closing, before `HandOverPayload`:

```ts
export type OrderRejectReason =
  | 'OutOfStock'
  | 'SoldElsewhere'
  | 'CannotDeliver'
  | 'WrongPrice'
  | 'Other'
```

- [ ] **Step 2: Add the labels map**

In `frontend/src/lib/orderStatus.ts`, add near the top (after the imports, before `ORDER_STATUS_LABELS`):

```ts
import type { OrderRejectReason, OrderStatus } from '../types'

export const ORDER_REJECT_REASON_LABELS: Record<OrderRejectReason, string> = {
  OutOfStock: 'Hết hàng',
  SoldElsewhere: 'Đã bán nơi khác',
  CannotDeliver: 'Không giao được',
  WrongPrice: 'Sai giá',
  Other: 'Khác',
}
```

Note: the file's existing first line is `import type { OrderStatus } from '../types'` — merge that into the new import line above rather than duplicating the import statement.

- [ ] **Step 3: Update the api.ts reject function**

In `frontend/src/features/orders/api.ts`, add `OrderRejectReason` to the type import list (line 2-12 block), then replace:

```ts
  reject: (id: string, reason: string) =>
    unwrap(apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/reject`, { reason })),
```

with:

```ts
  reject: (id: string, payload: { reasonCode: OrderRejectReason; reasonNote?: string }) =>
    unwrap(
      apiClient.post<ApiResponse<OrderDetail>>(`/orders/${id}/reject`, {
        reasonCode: payload.reasonCode,
        reasonNote: payload.reasonNote,
      }),
    ),
```

- [ ] **Step 4: Update OrderDetailPage.tsx state and mutation**

In `frontend/src/pages/OrderDetailPage.tsx`:

Add import: `import { ORDER_REJECT_REASON_LABELS } from '../lib/orderStatus'` (merge into the existing import block from `'../lib/orderStatus'` rather than adding a new import line).

Add state near `actionNote` (after line `const [actionNote, setActionNote] = useState('')`):

```tsx
const [rejectReasonCode, setRejectReasonCode] = useState<OrderRejectReason>('OutOfStock')
```

Add `OrderRejectReason` to the type import from `'../types'` (currently `import type { HandOverPayload } from '../types'`), making it `import type { HandOverPayload, OrderRejectReason } from '../types'`.

Replace:

```tsx
const rejectM = useMutation(act(() => ordersApi.reject(id, actionNote || 'Từ chối')))
```

with:

```tsx
const rejectM = useMutation(
  act(() =>
    ordersApi.reject(id, {
      reasonCode: rejectReasonCode,
      reasonNote: actionNote || undefined,
    }),
  ),
)
```

- [ ] **Step 5: Add the reason select UI, shown only when seller can reject**

Find the action panel block starting with:

```tsx
<div className="space-y-3 rounded-2xl border border-line bg-white/80 p-4">
  <Input
    label="Ghi chú hành động"
    value={actionNote}
    onChange={(e) => setActionNote(e.target.value)}
    placeholder="Lý do / ghi chú (nếu cần)"
  />
```

The seller reject buttons appear later in this same block (two `<Button variant="danger" onClick={() => rejectM.mutate()} ...>` at lines ~372 and ~388, guarded by `isSeller && o.status === 'PendingSellerConfirmation'` conditions — confirm exact condition text by reading the file before editing, since it may differ slightly from `AwaitingPayment`/`PendingConfirmation` naming used pre-PASSDO-04 rename). Insert the reason select right after the `Input` "Ghi chú hành động", still inside the same wrapping `<div>`, before the `<div className="flex flex-wrap gap-2">` buttons row:

```tsx
{isSeller && (o.status === 'AwaitingPayment' || o.status === 'PendingSellerConfirmation') && (
  <div>
    <Select
      label="Lý do từ chối (nếu từ chối đơn)"
      value={rejectReasonCode}
      onChange={(e) => setRejectReasonCode(e.target.value as OrderRejectReason)}
    >
      {Object.entries(ORDER_REJECT_REASON_LABELS).map(([code, label]) => (
        <option key={code} value={code}>
          {label}
        </option>
      ))}
    </Select>
    {rejectReasonCode === 'Other' && (
      <p className="mt-1 text-xs text-muted">
        Vui lòng nhập lý do cụ thể vào ô "Ghi chú hành động" ở trên.
      </p>
    )}
  </div>
)}
```

Check the exact `isSeller` variable name and the exact status literals used for the reject buttons' guard condition in the current file (post PASSDO-04 rename, statuses are `AwaitingPayment` and `PendingSellerConfirmation`) — match the select's visibility condition to the same condition already guarding the two reject buttons so the select only shows when reject is actually possible.

- [ ] **Step 6: Disable the reject button when Other has no note**

Find both `<Button variant="danger" onClick={() => rejectM.mutate()} disabled={rejectM.isPending}>` occurrences and change `disabled` to also check the note requirement:

```tsx
disabled={rejectM.isPending || (rejectReasonCode === 'Other' && !actionNote.trim())}
```

- [ ] **Step 7: Build the frontend**

Run: `cd frontend && npm run build`
Expected: PASS, no TypeScript errors.

- [ ] **Step 8: Manual smoke check (no automated FE tests in this repo)**

Confirm via `rg "reason:" frontend/src/pages/OrderDetailPage.tsx frontend/src/features/orders/api.ts` that no leftover free-text `reason` payload remains for reject (cancel/fail-delivery keep `reason` — only reject changes).

- [ ] **Step 9: Commit**

```bash
git add frontend/src/types/index.ts frontend/src/lib/orderStatus.ts frontend/src/features/orders/api.ts frontend/src/pages/OrderDetailPage.tsx
git commit -m "feat(PASSDO-06): add reject reason select and conditional note in order detail UI"
```

---

### Task 4: Docs + full verify

**Files:**
- Modify: `docs/issues/PASSDO-06-seller-confirm.md`

- [ ] **Step 1: Update the issue checklist**

In `docs/issues/PASSDO-06-seller-confirm.md`, under "Cần cập nhật thêm (chuyên nghiệp)", check off:

```
- [x] Enum lý do từ chối: hết hàng / đã bán nơi khác / không giao được / sai giá / khác
- [x] UI select lý do + ô "Khác"
- [x] Notification type riêng `OrderRejected` (hiện có thể gộp cancel)
- [x] Testcase integration cho confirm/reject/prepare (unit-level reject coverage added; confirm/prepare already covered)
```

Leave "Chặn tạo đơn thứ 2 khi product đang Reserved" checked as `[x]` if not already (it was completed in PASSDO-03 — verify current checkbox state before editing, don't overwrite if already marked).

Update "Hoàn thành khi" summary line to reflect reject reason is now standardized.

- [ ] **Step 2: Full verify**

Run: `dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj -v q`
Expected: all tests pass.

Run: `cd frontend && npm run build`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add docs/issues/PASSDO-06-seller-confirm.md
git commit -m "docs(PASSDO-06): mark reject reason standardization done"
```

---

## Spec coverage checklist

| Spec requirement | Task |
|---|---|
| `OrderRejectReason` enum | 1 |
| Label/formatter helper | 1 |
| `NotificationTypes.OrderRejected` | 1 |
| `RejectOrderCommand(ReasonCode, ReasonNote)` + validator | 2 |
| Handler formats `CancellationReason` + sends `OrderRejected` | 2 |
| Controller + `RejectOrderRequest` contract | 2 |
| Tests: 5 reasons, Other+note, validator Other-without-note, validator whitespace, validator max length, Cancel still uses OrderCancelled | 2 |
| FE type + labels + api.ts | 3 |
| FE reject select + conditional note + disabled state | 3 |
| Docs update | 4 |

## Self-review

- No placeholders — every step has exact code or exact search/replace targets.
- Task 2 Step 4 flags the one place where the implementer must cross-check `INotificationService.NotifyAsync`'s real signature rather than guess, since it wasn't read in full during planning — this is a pointer to verify, not a placeholder for missing plan content.
- Task 3 Step 5 flags the one place where the implementer must confirm exact `isSeller`/status literal names already in the file before wiring the select's visibility, to guarantee it matches the two existing reject buttons exactly.
- `CancelOrderCommand` and `FailDeliveryCommand` are explicitly untouched — confirmed by Task 2's regression test (`CancelOrder_ByBuyer_StillUsesOrderCancelledNotification`).
