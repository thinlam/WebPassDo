# PASSDO-04 — Chuẩn hóa trạng thái đơn hàng

## Trạng thái

- [x] Đã làm (2026-07-29)
- [ ] Đã cập nhật thêm

## Trạng thái đề xuất (roadmap)

| Trạng thái | Hiển thị |
| ---------- | -------- |
| PendingSellerConfirmation | Chờ người bán xác nhận |
| Confirmed | Người bán đã xác nhận |
| Preparing | Đang chuẩn bị hàng |
| ReadyForShipment | Đã chuẩn bị hàng |
| Shipping | Đang giao |
| Delivered | Đã giao |
| Completed | Hoàn tất |
| Cancelled | Đã hủy |
| Disputed | Đang khiếu nại |
| Refunded | Đã hoàn tiền |

## Hiện trạng source

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ `AwaitingPayment, PendingSellerConfirmation, Preparing, ReadyForShipment, Shipping, Delivered, Completed, Cancelled, DeliveryFailed, Returned, Refunded` |
| Frontend | ✅ Type union + label map + tabs + buyer complete button |
| Database | ✅ `OrderStatusHistories` + migration đổi 3 chuỗi cũ; `Orders.CompletedAt` nullable |
| API | ✅ POST /api/orders/{id}/complete (buyer/admin; idempotent) + tất cả action cũ |
| State machine | ✅ `OrderStatusTransitions` static helper (IsTerminal/IsActive/CanBuyerConfirmComplete/IsProductReserving) |
| Còn lại | `Disputed` (PASSDO-18); auto-complete background job (defer); Refunded flow (defer) |

## Mapping đề xuất (hiện tại → roadmap)

| Hiện tại | Roadmap gần nhất |
| -------- | ---------------- |
| PendingConfirmation | PendingSellerConfirmation |
| AwaitingPreparation | Confirmed / Preparing |
| AwaitingHandover | ReadyForShipment |
| Shipping | Shipping |
| Delivered | Delivered (+ cần thêm Completed) |
| Cancelled | Cancelled |
| Refunded | Refunded (chưa implement flow) |
| — | Disputed (chưa có) |

## Checklist nghiệp vụ

- [x] Người mua không tự confirm đơn (seller/admin)
- [x] Seller chỉ xử lý đơn sản phẩm của mình
- [x] Chặn chuyển sai thứ tự (ad-hoc trong từng handler)
- [x] Hủy có lý do (string)
- [x] Lưu lịch sử mỗi lần đổi status
- [x] Thêm `Completed` sau Delivered — buyer confirm; idempotent; CompletedAt
- [ ] Thêm `Disputed` (PASSDO-18 — defer)
- [x] State machine tập trung (`OrderStatusTransitions` static helper)

## Definition of Done

Trạng thái đúng luồng, API từ chối thao tác không hợp lệ, có timeline — **Done (core)**. Còn: Disputed (PASSDO-18), auto-complete background job, Refunded flow.
