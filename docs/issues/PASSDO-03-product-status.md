# PASSDO-03 — Chuẩn hóa trạng thái sản phẩm



## Trạng thái



- [x] Đã làm

- [x] Đã cập nhật thêm



## Trạng thái đề xuất (roadmap)



| Trạng thái | Ý nghĩa |

| ---------- | ------- |

| Draft | Bản nháp |

| PendingReview | Chờ kiểm duyệt |

| Active | Đang bán |

| Reserved | Đang có đơn xử lý |

| Sold | Đã bán |

| Hidden | Người bán tạm ẩn |

| Rejected | Bị từ chối |



## Hiện trạng source



| Hạng mục | Nội dung |

| -------- | -------- |

| Backend | ✅ Enum: `Draft, Active, Reserved, Sold, Hidden, Rejected, PendingReview`. `ProductStatusTransitions` (`CanSellerTransition`, `CanAdminTransition`, `IsPubliclyListable`). Create luôn `Draft`; bỏ qua status client |

| Frontend | ✅ Mirror enum C# (`Active`, `PendingReview`). Shared VN labels + seller actions (`features/products/status.ts`); My Products / detail / create dùng chung |

| Database | ✅ Cột `Status` (nvarchar/string) lưu **tên enum** (vd: `Active`, `Reserved`, `Sold`...). Data cũ có thể còn `Available` → remap sang `Active` bằng SQL trong migration `OneActiveOrderPerProduct`. |

| API | `PATCH /api/products/{id}/status` (seller/admin gates); order flow Active→Reserved / cancel→Active / Sold |

| Ngoài phạm vi | **Multi-stock inventory** và **admin moderation queue UI** — xem [design spec](../superpowers/specs/2026-07-29-product-status-design.md) |

| Việc tiếp theo | Admin products page (Approve/Reject UI); multi-stock follow-up issue |



## Checklist nghiệp vụ



- [x] Chỉ `Active` mới được đặt mua (public list/buy filter `Active`)

- [x] Có đơn hợp lệ → `Reserved` (create order)

- [x] Giao dịch xong → `Sold` (order completed path)

- [x] Hủy đơn → về `Active`

- [x] Không tạo nhiều đơn active cùng 1 sản phẩm (app check + `UX_Orders_OneActivePerProduct` index)

- [x] `PendingReview` + admin duyệt qua API (`PendingReview → Active/Rejected`); seller `Draft ↔ PendingReview`



## Đã làm (Tasks 1–5)



1. Rename `Available` → `Active` (=1); thêm `PendingReview` (=6).

2. Create luôn `Draft`; ignore client Active/Reserved/Sold/Rejected/PendingReview.

3. Transition matrices seller/admin trong `ProductStatusTransitions`.

4. Public list/buy chỉ `Active`; order flow Reserved/Sold/Active restore.

5. ≤1 non-terminal order/product + race-safe filtered unique index.

6. FE shared VN labels + seller status actions trên My Products / detail / create.



## Definition of Done



Frontend và Backend cùng một bộ trạng thái, không dùng chuỗi tùy ý — **đạt** (enum + labels đồng bộ; admin queue UI deferred).


