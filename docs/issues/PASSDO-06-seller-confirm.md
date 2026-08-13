# PASSDO-06 — Hoàn thiện luồng người bán xác nhận đơn

## Trạng thái

- [x] Đã làm (cốt lõi)
- [ ] Đã cập nhật thêm

## Luồng hiện có

1. Buyer tạo đơn → `PendingConfirmation` (sau thanh toán nếu COD/transfer theo flow)
2. Product → `Reserved`
3. Seller nhận noti `NewOrder`
4. Seller `confirm` hoặc `reject` (+ reason string)
5. Reject → product về Available; Cancel tương tự

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ Confirm / Reject / MarkPrepared / Cancel + ownership check |
| Frontend | ✅ Nút confirm/reject/prepare trên OrderDetail |
| Database | ✅ Order + StatusHistory; Reason lưu note/history |
| API | `POST /api/orders/{id}/confirm`, `/reject`, `/mark-prepared`, `/cancel`, `/hand-over` |
| Lỗi hiện tại | Endpoint tên hơi khác roadmap (`mark-prepared` vs `start-preparing` / `ready-for-shipment`) — deferred |
| Việc tiếp theo | Mapping API names nếu cần (deferred) |

## API roadmap vs hiện tại

| Roadmap | Hiện tại |
| ------- | -------- |
| `POST .../confirm` | ✅ Có |
| `POST .../reject` | ✅ Có |
| `POST .../start-preparing` | ⚠️ Confirm đưa vào `AwaitingPreparation`; prepare = `mark-prepared` |
| `POST .../ready-for-shipment` | ⚠️ Gộp vào `mark-prepared` → `AwaitingHandover` |
| `POST .../cancel` | ✅ Có |

## Đã làm

- [x] Chỉ chủ sản phẩm (seller) / admin confirm
- [x] Buyer nhận kết quả qua notification + xem order
- [x] Lý do từ chối chuẩn hóa (enum `OrderRejectReason` + note khi "Khác")
- [x] Product về Active khi reject/cancel

## Cần cập nhật thêm (chuyên nghiệp)

- [x] Enum lý do từ chối: hết hàng / đã bán nơi khác / không giao được / sai giá / khác (`OrderRejectReason`)
- [x] UI select lý do + ô "Khác" (bắt buộc note khi chọn Khác)
- [x] Notification type riêng `OrderRejected` (tách khỏi `OrderCancelled`)
- [x] Chặn tạo đơn thứ 2 khi product đang Reserved (unique active order constraint — hoàn thành ở PASSDO-03)
- [x] Unit test coverage cho reject (5 lý do, validator Other, format, regression Cancel vẫn dùng OrderCancelled)
- [ ] Rename API endpoint theo roadmap (`start-preparing` / `ready-for-shipment`) — deferred, không breaking cần thiết

## Hoàn thành khi

Chỉ chủ SP confirm; buyer nhận kết quả; lý do chuẩn hóa (enum + note); SP về Active khi từ chối — **Done**. Còn: rename endpoint theo roadmap (deferred, không ảnh hưởng nghiệp vụ).
