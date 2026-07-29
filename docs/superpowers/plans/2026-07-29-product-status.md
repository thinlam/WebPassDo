# PASSDO-03 Product Status Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Align BE/FE product statuses (`Active` + `PendingReview`), enforce moderation and order gates for one-unit listings, and localize status UI.

**Architecture:** Rename enum member `Available`→`Active` (value `1` unchanged), append `PendingReview = 6`. Centralize allowed transitions in an Application helper used by create/update-status handlers. Order create requires `Active`, sets `Reserved`, enforces ≤1 non-terminal order per product via app check + SQL filtered unique index. FE shares one VN label/action map; no admin products page exists today so admin Approve/Reject is API+tests only.

**Tech Stack:** ASP.NET Core / EF Core / MediatR / FluentValidation / xUnit+FluentAssertions+Moq; React + TypeScript + TanStack Query; SQL Server filtered unique index.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-29-product-status-design.md`
- Enum ints: `Draft=0`, `Active=1`, `Reserved=2`, `Sold=3`, `Hidden=4`, `Rejected=5`, `PendingReview=6`
- Do **not** remap existing `Status=1` rows
- Create always `Draft`; ignore client Active/Reserved/Sold/Rejected/PendingReview on create
- Multi-stock inventory (ReservedQuantity etc.) is **out of scope**
- No full admin moderation queue UI (only `AdminCategoriesPage` exists)
- YAGNI: prefer constrained `PATCH .../status` over new approve/reject endpoints unless existing API cannot express the matrix cleanly
- Commits: small, conventional (`feat(PASSDO-03): …` / `test(PASSDO-03): …` / `docs(PASSDO-03): …`)

---

## File map

| File | Responsibility |
|------|----------------|
| `backend/src/PassDo.Domain/Enums/ProductStatus.cs` | Canonical enum |
| `backend/src/PassDo.Application/Products/ProductStatusTransitions.cs` | Allowed transition matrix + helpers |
| `backend/.../CreateProduct/CreateProductCommand.cs` | Force Draft |
| `backend/.../UpdateProductStatus/UpdateProductStatusCommand.cs` | Seller/admin gates |
| `backend/.../UpdateProduct/UpdateProductCommand.cs` | Status field constraints if present |
| `backend/.../CreateOrder/CreateOrderCommand.cs` | Active-only buy; global one-order; Active→Reserved |
| `backend/.../OrderActions/OrderActionCommands.cs` | Reserved→Active / Sold (rename Available) |
| `backend/.../GetProducts/GetProductsQuery.cs` | Public default Active |
| `backend/.../GetProductById/GetProductByIdQuery.cs` | Visibility rules |
| `backend/.../Favorites/...` | Block non-public statuses |
| `backend/.../DatabaseInitializer.cs` | Seed Active |
| `backend/.../Migrations/*_OneActiveOrderPerProduct.cs` | Filtered unique index |
| `backend/tests/PassDo.UnitTests/Products/ProductStatusTests.cs` | Status transition + create tests |
| `backend/tests/PassDo.UnitTests/Orders/*` | Buy / reservation / second-order tests |
| `frontend/src/types/index.ts` | `ProductStatus` union |
| `frontend/src/features/products/status.ts` | Labels, badges, allowed actions |
| `frontend/src/pages/{Create,Edit,My,ProductDetail}*.tsx` | UI wiring |
| `docs/issues/PASSDO-03-product-status.md` | Close checklist / matrix |

---

### Task 1: Domain enum + transition helper + create defaults

**Files:**
- Modify: `backend/src/PassDo.Domain/Enums/ProductStatus.cs`
- Create: `backend/src/PassDo.Application/Products/ProductStatusTransitions.cs`
- Modify: `backend/src/PassDo.Application/Products/Commands/CreateProduct/CreateProductCommand.cs`
- Test: `backend/tests/PassDo.UnitTests/Products/ProductStatusTests.cs`
- Modify: `backend/tests/PassDo.UnitTests/Products/CategoryAndProductHandlerTests.cs` (rename Available→Active expectations only after Task 1 behavior change)

**Interfaces:**
- Consumes: `ProductStatus` enum
- Produces:
  - `ProductStatusTransitions.CanSellerTransition(ProductStatus from, ProductStatus to) -> bool`
  - `ProductStatusTransitions.CanAdminTransition(ProductStatus from, ProductStatus to) -> bool`
  - `ProductStatusTransitions.IsSystemManaged(ProductStatus status) -> bool` (`Reserved`/`Sold`)
  - `ProductStatusTransitions.IsPubliclyListable(ProductStatus status) -> bool` (`Active` only)

- [ ] **Step 1: Write failing tests for create + transitions**

Create `backend/tests/PassDo.UnitTests/Products/ProductStatusTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PassDo.Application.Categories.Commands.CreateCategory;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Products;
using PassDo.Application.Products.Commands.CreateProduct;
using PassDo.Domain.Enums;
using PassDo.Infrastructure.Persistence;
using PassDo.Infrastructure.Services;

namespace PassDo.UnitTests.Products;

public class ProductStatusTests
{
    private static PassDoDbContext CreateDb(Guid? userId = null, string role = "User")
    {
        var options = new DbContextOptionsBuilder<PassDoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(userId.HasValue);
        currentUser.Setup(x => x.Role).Returns(role);
        var dateTime = new Mock<IDateTimeProvider>();
        dateTime.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        return new PassDoDbContext(options, currentUser.Object, dateTime.Object);
    }

    [Theory]
    [InlineData(ProductStatus.Draft, ProductStatus.PendingReview, true)]
    [InlineData(ProductStatus.PendingReview, ProductStatus.Draft, true)]
    [InlineData(ProductStatus.Rejected, ProductStatus.Draft, true)]
    [InlineData(ProductStatus.Active, ProductStatus.Hidden, true)]
    [InlineData(ProductStatus.Hidden, ProductStatus.Active, true)]
    [InlineData(ProductStatus.Draft, ProductStatus.Active, false)]
    [InlineData(ProductStatus.PendingReview, ProductStatus.Active, false)]
    [InlineData(ProductStatus.Rejected, ProductStatus.PendingReview, false)]
    public void Seller_transitions_matrix(ProductStatus from, ProductStatus to, bool expected)
    {
        ProductStatusTransitions.CanSellerTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData(ProductStatus.PendingReview, ProductStatus.Active, true)]
    [InlineData(ProductStatus.PendingReview, ProductStatus.Rejected, true)]
    [InlineData(ProductStatus.Draft, ProductStatus.Active, false)]
    public void Admin_transitions_matrix(ProductStatus from, ProductStatus to, bool expected)
    {
        ProductStatusTransitions.CanAdminTransition(from, to).Should().Be(expected);
    }

    [Fact]
    public async Task CreateProduct_AlwaysDraft_IgnoresClientActive()
    {
        var sellerId = Guid.NewGuid();
        await using var db = CreateDb(sellerId);
        var category = await new CreateCategoryCommandHandler(db).Handle(
            new CreateCategoryCommand("Cat", null, "cat", 1, true), CancellationToken.None);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(sellerId);
        currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        currentUser.Setup(x => x.Role).Returns("User");

        var result = await new CreateProductCommandHandler(db, currentUser.Object).Handle(
            new CreateProductCommand(
                "Item", "Desc", 100, 50, ProductCondition.LikeNew, category.Id,
                "HCM", 1, null, null, AcceptedPaymentOption.CashOnDelivery,
                new[] { DeliverySpeed.Standard }, ProductStatus.Active),
            CancellationToken.None);

        result.Status.Should().Be(nameof(ProductStatus.Draft));
    }
}
```

- [ ] **Step 2: Run tests — expect fail**

Run:

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj --filter "FullyQualifiedName~ProductStatusTests" -v n
```

Expected: compile fail (`ProductStatusTransitions` missing and/or `PendingReview` / `Active` missing) or assertion fail (`Available` still returned / Active honored on create).

- [ ] **Step 3: Implement enum + helper + create force Draft**

`ProductStatus.cs`:

```csharp
namespace PassDo.Domain.Enums;

public enum ProductStatus
{
    Draft = 0,
    Active = 1,
    Reserved = 2,
    Sold = 3,
    Hidden = 4,
    Rejected = 5,
    PendingReview = 6
}
```

`ProductStatusTransitions.cs`:

```csharp
using PassDo.Domain.Enums;

namespace PassDo.Application.Products;

public static class ProductStatusTransitions
{
    public static bool IsSystemManaged(ProductStatus status) =>
        status is ProductStatus.Reserved or ProductStatus.Sold;

    public static bool IsPubliclyListable(ProductStatus status) =>
        status == ProductStatus.Active;

    public static bool CanSellerTransition(ProductStatus from, ProductStatus to) =>
        (from, to) switch
        {
            (ProductStatus.Draft, ProductStatus.Draft) => true,
            (ProductStatus.Draft, ProductStatus.PendingReview) => true,
            (ProductStatus.PendingReview, ProductStatus.Draft) => true,
            (ProductStatus.Rejected, ProductStatus.Draft) => true,
            (ProductStatus.Active, ProductStatus.Hidden) => true,
            (ProductStatus.Hidden, ProductStatus.Active) => true,
            _ => false
        };

    public static bool CanAdminTransition(ProductStatus from, ProductStatus to) =>
        (from, to) switch
        {
            (ProductStatus.PendingReview, ProductStatus.Active) => true,
            (ProductStatus.PendingReview, ProductStatus.Rejected) => true,
            _ => false
        };
}
```

In `CreateProductCommand`:
- Remove Status validator allowing Available/Hidden, or restrict optional Status to Draft only (YAGNI: ignore Status entirely for create).
- Handler: `Status = ProductStatus.Draft` always (do not use `request.Status`).

- [ ] **Step 4: Run tests — expect pass**

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj --filter "FullyQualifiedName~ProductStatusTests" -v n
```

Expected: PASS.

- [ ] **Step 5: Fix broken Available references in product tests that fail to compile; leave order tests for Task 3**

Replace `ProductStatus.Available` with `ProductStatus.Active` in files that no longer compile after the rename (especially `CategoryAndProductHandlerTests` — update assertion to expect Draft after create-with-client-Active, or stop passing Active and expect Draft when Status null).

- [ ] **Step 6: Commit**

```powershell
git add backend/src/PassDo.Domain/Enums/ProductStatus.cs `
  backend/src/PassDo.Application/Products/ProductStatusTransitions.cs `
  backend/src/PassDo.Application/Products/Commands/CreateProduct/CreateProductCommand.cs `
  backend/tests/PassDo.UnitTests/Products/ProductStatusTests.cs `
  backend/tests/PassDo.UnitTests/Products/CategoryAndProductHandlerTests.cs
git commit -m "feat(PASSDO-03): add Active/PendingReview enum and force create Draft"
```

---

### Task 2: UpdateProductStatus gates + public query filters + seed

**Files:**
- Modify: `backend/src/PassDo.Application/Products/Commands/UpdateProductStatus/UpdateProductStatusCommand.cs`
- Modify: `backend/src/PassDo.Application/Products/Commands/UpdateProduct/UpdateProductCommand.cs`
- Modify: `backend/src/PassDo.Application/Products/Queries/GetProducts/GetProductsQuery.cs`
- Modify: `backend/src/PassDo.Application/Products/Queries/GetProductById/GetProductByIdQuery.cs`
- Modify: `backend/src/PassDo.Application/Favorites/Commands/AddFavorite/AddFavoriteCommand.cs`
- Modify: `backend/src/PassDo.Infrastructure/Persistence/DatabaseInitializer.cs`
- Modify: `backend/tests/PassDo.UnitTests/Products/ProductStatusTests.cs` (append handler tests)

**Interfaces:**
- Consumes: `ProductStatusTransitions.*`
- Produces: Status PATCH that enforces seller/admin matrix; public list defaults to Active

- [ ] **Step 1: Add failing handler tests**

Append to `ProductStatusTests.cs` tests that:

1. Seller cannot `PendingReview → Active` (Forbidden/Conflict).
2. Admin can `PendingReview → Active`.
3. Seller can `Draft → PendingReview`.

Seed `Category` + `Product` entities directly on the in-memory `PassDoDbContext` (same pattern as existing unit tests).

- [ ] **Step 2: Run filter — expect fail**

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj --filter "FullyQualifiedName~ProductStatusTests" -v n
```

- [ ] **Step 3: Implement UpdateProductStatus gates**

Replace permissive assignment with transition checks using `ProductStatusTransitions`. Keep Sold immutability. Block system-managed targets (`Reserved`/`Sold`) on PATCH.

In `UpdateProductCommand`: if Status is provided, validate via `CanSellerTransition` (or drop Status from update and force PATCH only).

Replace all `ProductStatus.Available` with `ProductStatus.Active` in GetProducts public default, GetProductById visibility, Favorites, DatabaseInitializer. Prefer `ProductStatusTransitions.IsPubliclyListable` for public checks.

- [ ] **Step 4: Run tests — expect pass**

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj --filter "FullyQualifiedName~ProductStatusTests" -v n
```

- [ ] **Step 5: Build solution for remaining Available compile errors; fix product handlers only**

```bash
dotnet build backend/PassDo.sln
```

Fix any remaining `Available` in Products application layer (not Orders — Task 3).

- [ ] **Step 6: Commit**

```powershell
git add backend/src/PassDo.Application/Products backend/src/PassDo.Application/Favorites backend/src/PassDo.Infrastructure/Persistence/DatabaseInitializer.cs backend/tests/PassDo.UnitTests/Products
git commit -m "feat(PASSDO-03): enforce product status transition gates"
```

---

### Task 3: Order flow Active/Reserved + one-order constraint + race index

**Files:**
- Modify: `backend/src/PassDo.Application/Orders/Commands/CreateOrder/CreateOrderCommand.cs`
- Modify: `backend/src/PassDo.Application/Orders/Commands/OrderActions/OrderActionCommands.cs`
- Create: EF migration under `backend/src/PassDo.Infrastructure/Persistence/Migrations/` (filtered unique index)
- Modify: `backend/tests/PassDo.UnitTests/Orders/FavoriteAndOrderHandlerTests.cs` (and/or new `ProductOrderReservationTests.cs`)

**Interfaces:**
- Consumes: `ProductStatus.Active`, `OrderStatusGroups.ActiveProcessing`
- Produces: buy only Active; Active→Reserved; cancel→Active; ≤1 active order/product; SQL UX index

- [ ] **Step 1: Write failing order reservation tests**

Cover:

1. `CreateOrder` on Active sets Reserved.
2. Non-Active product rejected.
3. Second non-terminal order for same ProductId (different buyer) rejected — remove BuyerId-only semantics.
4. Cancel restores Active (update Available→Active assertion if present).

Use existing FavoriteAndOrderHandlerTests scaffolding (shipping/notification mocks).

- [ ] **Step 2: Run — expect fail**

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj --filter "FullyQualifiedName~CreateOrder|ProductOrderReservation|FavoriteAndOrder" -v n
```

- [ ] **Step 3: Implement CreateOrder + cancel/complete renames**

In `CreateOrderCommand.cs`:

- Require `ProductStatus.Active`.
- `hasActiveOrder` filter: `ProductId` + `OrderStatusGroups.ActiveProcessing` — **no** `BuyerId`.
- Set `product.Status = ProductStatus.Reserved` on success.
- Replace every `ProductStatus.Available` in OrderActionCommands with `ProductStatus.Active`.

Add migration SQL (SQL Server):

```csharp
migrationBuilder.Sql(@"
CREATE UNIQUE INDEX UX_Orders_OneActivePerProduct
ON Orders(ProductId)
WHERE IsDeleted = 0 AND [Status] IN (0,1,2,3,4);
");
```

Down:

```csharp
migrationBuilder.Sql(@"DROP INDEX UX_Orders_OneActivePerProduct ON Orders;");
```

Generate via:

```bash
dotnet ef migrations add OneActiveOrderPerProduct --project backend/src/PassDo.Infrastructure --startup-project backend/src/PassDo.Api --output-dir Persistence/Migrations
```

Then edit Up/Down to include the filtered index SQL.

**Race note:** App check + Status Active→Reserved in same `SaveChanges` + filtered unique index. InMemory tests cover sequential second-order failure; index protects concurrent SQL inserts. Do not add a flaky parallel InMemory test.

- [ ] **Step 4: Run order + full unit tests**

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj -v n
```

Expected: PASS. Fix leftover Available compile errors solution-wide.

- [ ] **Step 5: Commit**

```powershell
git add backend/src/PassDo.Application/Orders backend/src/PassDo.Infrastructure/Persistence/Migrations backend/tests/PassDo.UnitTests/Orders
git commit -m "feat(PASSDO-03): reserve Active products with one active order"
```

---

### Task 4: Frontend types, shared labels/actions, screens

**Files:**
- Modify: `frontend/src/types/index.ts`
- Create: `frontend/src/features/products/status.ts`
- Modify: `frontend/src/features/products/api.ts` (optional submit helper → `updateStatus(id, 'PendingReview')`)
- Modify: `frontend/src/pages/CreateProductPage.tsx`
- Modify: `frontend/src/pages/EditProductPage.tsx`
- Modify: `frontend/src/pages/MyProductsPage.tsx`
- Modify: `frontend/src/pages/ProductDetailPage.tsx`
- Grep-fix any other `Available` in `frontend/`

**Interfaces:**
- Consumes: API status strings matching C# names
- Produces:
  - `export type ProductStatus = 'Draft' | 'Active' | 'Reserved' | 'Sold' | 'Hidden' | 'Rejected' | 'PendingReview'`
  - `PRODUCT_STATUS_LABELS: Record<ProductStatus, string>`
  - `sellerStatusActions(status: ProductStatus): { label: string; next: ProductStatus }[]`

- [ ] **Step 1: Add `status.ts` and update types**

```ts
import type { ProductStatus } from '../../types'

export const PRODUCT_STATUS_LABELS: Record<ProductStatus, string> = {
  Draft: 'Bản nháp',
  Active: 'Đang bán',
  Reserved: 'Đã được giữ',
  Sold: 'Đã bán',
  Hidden: 'Đã ẩn',
  Rejected: 'Bị từ chối',
  PendingReview: 'Chờ duyệt',
}

export function formatProductStatus(status: string): string {
  return PRODUCT_STATUS_LABELS[status as ProductStatus] ?? status
}

export function sellerStatusActions(status: ProductStatus): { label: string; next: ProductStatus }[] {
  switch (status) {
    case 'Draft':
      return [{ label: 'Gửi duyệt', next: 'PendingReview' }]
    case 'PendingReview':
      return [{ label: 'Rút duyệt', next: 'Draft' }]
    case 'Rejected':
      return [{ label: 'Về bản nháp', next: 'Draft' }]
    case 'Active':
      return [{ label: 'Ẩn', next: 'Hidden' }]
    case 'Hidden':
      return [{ label: 'Hiện lại', next: 'Active' }]
    default:
      return []
  }
}

export function canBuyStatus(status: string): boolean {
  return status === 'Active'
}
```

Update `types/index.ts` ProductStatus union; remove `Available`.

- [ ] **Step 2: Wire Create/Edit — no Active picker; default Draft**

- CreateProductPage: remove status select for Available/Active; omit status or send Draft only.
- EditProductPage: remove Active from seller select; prefer My Products action buttons for transitions.
- After create success, optional CTA “Gửi duyệt” via `productsApi.updateStatus(id, 'PendingReview')`.

- [ ] **Step 3: Wire MyProducts + ProductDetail**

- MyProductsPage: `formatProductStatus`; buttons from `sellerStatusActions`.
- ProductDetailPage: shared labels; buy when `canBuyStatus(product.status)`.

- [ ] **Step 4: Grep cleanup**

```bash
rg "Available" frontend/src
```

Expected: no product-status `Available` left.

- [ ] **Step 5: Typecheck / build frontend**

```bash
cd frontend
npm run build
```

Expected: SUCCESS (run `npm install` first if deps missing).

- [ ] **Step 6: Commit**

```powershell
git add frontend/src
git commit -m "feat(PASSDO-03): sync FE product statuses and VN labels"
```

---

### Task 5: Docs + solution verify

**Files:**
- Modify: `docs/issues/PASSDO-03-product-status.md`
- Modify: `docs/issues/passdo-current-status.md` (PASSDO-03 row)

- [ ] **Step 1: Update issue checklist**

Mark enum alignment, PendingReview gates, FE labels, one-order constraint; note multi-stock / admin queue out of scope with link to design spec.

- [ ] **Step 2: Full verify**

```bash
dotnet test backend/tests/PassDo.UnitTests/PassDo.UnitTests.csproj -v n
cd frontend
npm run build
rg "ProductStatus\.Available|status === 'Available'" backend frontend/src
```

Expected: tests pass; FE build pass; no product Available left outside historical noise.

- [ ] **Step 3: Commit docs**

```powershell
git add docs/issues
git commit -m "docs(PASSDO-03): mark product status standardization done"
```

---

## Spec coverage checklist

| Spec requirement | Task |
|------------------|------|
| Rename Available→Active (=1), PendingReview=6 | 1 |
| Create always Draft; ignore client Active | 1 |
| Transition matrices seller/admin | 1–2 |
| Public list/buy Active only | 2–3 |
| Order Active→Reserved / cancel→Active / Sold | 3 |
| ≤1 non-terminal order/product + race (index) | 3 |
| FE shared VN labels + actions | 4 |
| Admin queue UI deferred; API gates only | 2 (no new admin page) |
| Multi-stock out of scope | documented Task 5 |
| Tests listed in spec | 1–3 |
| Issue docs update | 5 |

## Placeholder / consistency self-review

- No TBD steps; enum ints match spec.
- Helper names `CanSellerTransition` / `CanAdminTransition` reused in Task 2.
- Frontend `PendingReview` / `Active` names match C# `JsonStringEnumConverter` output.
- Filtered index statuses `0..4` match `OrderStatusGroups.ActiveProcessing`.
