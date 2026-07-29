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
| Lỗi hiện tại | Lý do từ chối là free-text, chưa enum chuẩn; endpoint tên hơi khác roadmap (`mark-prepared` vs `start-preparing` / `ready-for-shipment`) |
| Việc tiếp theo | Chuẩn hóa reject reasons + mapping API names nếu cần |

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
- [x] Lý do từ chối bắt buộc (string)
- [x] Product về Available khi reject/cancel

## Cần cập nhật thêm (chuyên nghiệp)

- [ ] Enum lý do từ chối: hết hàng / đã bán nơi khác / không giao được / sai giá / khác
- [ ] UI select lý do + ô “Khác”
- [ ] Notification type riêng `OrderRejected` (hiện có thể gộp cancel)
- [ ] Chặn tạo đơn thứ 2 khi product đang Reserved (unique active order constraint)
- [ ] Testcase integration cho confirm/reject/prepare

## Hoàn thành khi

Chỉ chủ SP confirm; buyer nhận kết quả; lý do lưu; SP về Active khi từ chối — **cốt lõi ✅**, chuẩn hóa lý do còn thiếu.
