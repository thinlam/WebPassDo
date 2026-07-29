# PASSDO-07 — Hoàn thiện hệ thống thông báo

## Trạng thái

- [x] Đã làm (cốt lõi)
- [ ] Đã cập nhật thêm

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ Entity Notification, mark read, unread count, SignalR push |
| Frontend | ✅ NotificationBell, realtime bridge, poll fallback |
| Database | ✅ Bảng `Notifications` |
| API | `GET /api/notifications`, `/unread-count`, `POST /{id}/read`, `/read-all` |
| Lỗi hiện tại | Thiếu một số loại noti roadmap; deep-link Product yếu; `OrderShipping` constant chưa dùng |
| Việc tiếp theo | Bổ sung types + đảm bảo ActionUrl luôn có |

## Coverage thông báo

### Người bán

| Sự kiện | Hiện tại |
| ------- | -------- |
| Có người đặt mua | ✅ NewOrder |
| Người mua hủy | ✅ OrderCancelled |
| Người mua xác nhận đã nhận | ✅ OrderDelivered (cần verify actor) |
| Người mua mở khiếu nại | ❌ Chưa (PASSDO-18) |
| Người mua gửi đánh giá | ❌ Chưa (PASSDO-13) |

### Người mua

| Sự kiện | Hiện tại |
| ------- | -------- |
| Seller xác nhận | ✅ OrderConfirmed |
| Seller từ chối | ⚠️ Có thể qua cancel/reject — cần type riêng |
| Đang chuẩn bị | ✅ OrderPrepared |
| Đã chuẩn bị / bàn giao | ✅ OrderHandedOver |
| Đang giao | ⚠️ Constant có, handler chưa chắc fire |
| Đơn bị hủy | ✅ OrderCancelled |
| Khiếu nại đã xử lý | ❌ Chưa |

## Đã làm

- [x] Lưu DB
- [x] Realtime SignalR
- [x] Unread count
- [x] Đánh dấu đã đọc
- [x] Click mở order (khi có actionUrl / related Order)

## Cần cập nhật thêm (chuyên nghiệp)

- [ ] Thêm types: OrderRejected, DisputeOpened, DisputeResolved, NewReview
- [ ] Luôn set `ActionUrl` + fallback Product → `/products/:id`
- [ ] Authorization: user chỉ đọc noti của mình (verify integration test)
- [ ] Empty / skeleton state trong dropdown
- [ ] Nhóm theo ngày; relative time VN

## Hoàn thành khi

Noti còn sau reload; không đọc được noti người khác — **cốt lõi ✅**, đủ loại sự kiện còn thiếu.
