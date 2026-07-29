# PASSDO-03 — Product Status Standardization Design

Date: 2026-07-29  
Status: Approved in conversation (awaiting file review)  
Scope: Align product status enum + transition gates + FE labels; one-unit listing model only

## Goal

Frontend and Backend share one product status set. Sellers cannot self-publish to Active. PendingReview enables basic moderation. Public buy/list only see Active. Concurrent purchases cannot double-reserve the same listing.

## Decisions

| Topic | Choice |
|-------|--------|
| Naming | Rename `Available` → `Active` (keep int value `1`) |
| PendingReview | Add as `6` (append; no remap of existing rows) |
| Create default | Always `Draft`; ignore client Active/Reserved/Sold/Rejected/PendingReview |
| Submit for review | Explicit action `Draft → PendingReview` |
| Hidden restore | `Hidden → Active` (seller-hidden only; already approved) |
| Multi-stock inventory | **Out of scope** — follow-up issue |
| Concurrent orders (this issue) | ≤1 non-terminal order per `ProductId`; race-safe Active→Reserved |
| Admin queue UI | Minimal Approve/Reject if page exists; else API+tests only |

## Enum

```csharp
public enum ProductStatus
{
    Draft = 0,
    Active = 1,          // was Available
    Reserved = 2,
    Sold = 3,
    Hidden = 4,
    Rejected = 5,
    PendingReview = 6
}
```

TypeScript mirror must match names exactly. JSON serializes enum as strings (`"Active"`, `"PendingReview"`).

**DB:** `Products.Status` remains `int`. Rows with value `1` stay `1` (now Active). No Status data migration rewrite.

**Cleanup:** Remove all `Available` references from BE, FE, tests, fixtures, mocks, seed, `DatabaseInitializer`, and issue docs.

## Transition matrix

### Seller

| From | To | Meaning |
|------|----|---------|
| Draft | Draft | Save draft |
| Draft | PendingReview | Submit for review |
| PendingReview | Draft | Withdraw submission |
| Rejected | Draft | Fix then re-submit later |
| Active | Hidden | Temporarily hide |
| Hidden | Active | Re-show (seller-hidden + previously approved) |

Seller **cannot** set: Active (except via Hidden→Active), Reserved, Sold, Rejected, or PendingReview from Rejected without Draft first.

### Admin

| From | To | Meaning |
|------|----|---------|
| PendingReview | Active | Approve |
| PendingReview | Rejected | Reject |

Admin **cannot** invent Reserved/Sold via generic status patch.

**Hidden** is seller-controlled in this issue. Do not reuse Hidden for admin suspension (needs future `Suspended` / `HiddenBy` / `CanSellerRestore`).

### System (order flow — one-unit model)

| Event | Status effect |
|-------|----------------|
| Create order (success) | Active → Reserved |
| Cancel / reject / expire reservation | Reserved → Active |
| Completed sale | Reserved → Sold |

Buy allowed only when `Status == Active`.

## One-unit listing model (in scope)

Each Product is one sellable listing unit (existing `Product.Quantity` field is not treated as multi-stock inventory here).

On successful order create:

1. Product must be `Active`.
2. Set product `Reserved`.
3. At most **one** non-terminal order per `ProductId` (remove `BuyerId` from the duplicate active-order check).

**Race safety:** Concurrent purchases for the same product must not both succeed. Preferred approach: transaction + conditional update (`WHERE Status = Active` then set Reserved) and/or concurrency token so only one Active→Reserved wins; loser gets conflict.

Terminal vs non-terminal order statuses: use existing central `OrderStatusGroups` (or equivalent). Non-terminal = still holding the listing.

## Public visibility

Public listing / search / favorites eligibility / buy: **Active only**.

Draft, PendingReview, Rejected, Hidden: not publicly purchasable; owner and authorized admin may still open management/detail where authorized today.

## API / commands

| Area | Change |
|------|--------|
| `CreateProduct` | Force `Draft`; do not honor forbidden client statuses |
| Explicit submit | Draft → PendingReview (dedicated command or constrained status update) |
| `UpdateProductStatus` | Enforce seller/admin transition matrix; keep Reserved/Sold system-only |
| `CreateOrder` | Require Active; set Reserved; global one active-order check; race protection |
| Order cancel/release | Reserved → Active (existing path, rename Available→Active) |
| Order complete/deliver | Reserved → Sold (existing path) |
| `GetProducts` public default | Filter `Active` (was Available) |

Optional later: dedicated `POST .../submit-review`, `POST .../approve`, `POST .../reject` — acceptable if clearer; otherwise constrained patch is enough for this issue.

## Frontend

Screens: My Products, Product detail, Create, Edit, Favorites, public list, buy flow, existing admin product page if any.

- Shared VN label map + badge helpers + allowed-action helpers + public-visibility checks (one source, no duplicated string maps).
- No raw enum display on user surfaces.
- Create/edit: no seller picker for Active; explicit “Submit for review”.
- PendingReview: read-only pending presentation; actions match matrix.

### Suggested VN labels

| Status | Label |
|--------|-------|
| Draft | Bản nháp |
| Active | Đang bán |
| Reserved | Đã được giữ |
| Sold | Đã bán |
| Hidden | Đã ẩn |
| Rejected | Bị từ chối |
| PendingReview | Chờ duyệt |

### Admin surface

Full moderation queue UI is **out of scope**. If an admin products page already exists, add minimal Approve/Reject for PendingReview. Otherwise ship API authorization + tests; open follow-up for queue UI.

## Tests (required)

- Create defaults to Draft; client cannot create as Active
- Draft → PendingReview submit; seller cannot self-approve to Active
- Admin PendingReview → Active / Rejected
- Public queries return only Active
- Buy rejects non-Active
- Order create Active → Reserved; cancel → Active; complete → Sold
- Second non-terminal order for same ProductId rejected
- Concurrent purchase: only one succeeds
- API/TS use `Active`, not `Available`
- C# and TS status sets stay in sync

## Documentation

Update `docs/issues/PASSDO-03-product-status.md` (and current-status matrix row) with final enum, transition table, and scope boundary.

## Out of scope (follow-up issue)

Multi-stock inventory model:

- StockQuantity / ReservedQuantity / SoldQuantity / AvailableQuantity formula
- Multiple concurrent orders while stock remains
- Atomic qty reserve/release, oversell tests
- Migrating listings to explicit `StockQuantity = 1`

Also out of scope:

- Full admin moderation queue UI
- `Suspended`, `HiddenBy`, split seller-hidden vs admin-hidden

## Definition of Done

- [ ] BE + FE share Draft, Active, Reserved, Sold, Hidden, Rejected, PendingReview
- [ ] No remaining `Available` in product status
- [ ] Seller cannot publish Active except Hidden→Active restore
- [ ] Create → Draft; submit → PendingReview; admin approve/reject
- [ ] Public buy/list = Active only
- [ ] ≤1 non-terminal order per ProductId with race protection
- [ ] Shared VN labels on relevant screens
- [ ] Tests + PASSDO-03 issue doc updated
- [ ] Multi-stock / full moderation UI tracked as follow-up, not silently deferred without note
