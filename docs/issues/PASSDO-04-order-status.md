# PASSDO-04 — Chuẩn hóa trạng thái đơn hàng

## Trạng thái

- [ ] Đã làm
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
| Backend | ⚠️ `AwaitingPayment, PendingConfirmation, AwaitingPreparation, AwaitingHandover, Shipping, Delivered, Cancelled, DeliveryFailed, Returned, Refunded` |
| Frontend | ✅ Timeline `statusHistory`; label map theo enum hiện tại |
| Database | ✅ `OrderStatusHistories` (Old/New/ChangedBy/Note) |
| API | Confirm/reject/prepare/hand-over/deliver/fail/cancel — validate status trước khi chuyển |
| Lỗi hiện tại | Naming lệch roadmap; **thiếu `Completed`, `Disputed`**; `Refunded` có enum nhưng chưa có flow set; không có bảng transition trung tâm |
| Việc tiếp theo | Map/rename enum + thêm Completed/Disputed + state machine table |

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
- [ ] Thêm `Completed` sau Delivered (buyer/seller confirm settle)
- [ ] Thêm `Disputed` (PASSDO-18)
- [ ] State machine tập trung (tránh logic rải rác)

## Definition of Done

Trạng thái đúng luồng, API từ chối thao tác không hợp lệ, có timeline — **partial** (timeline + guard có; naming/Completed/Disputed chưa đạt).
