# PASSDO-05 — Sửa lỗi DeliveryCompany bắt buộc

## Trạng thái

- [x] Đã làm (cốt lõi — lỗi field name đã fix)
- [ ] Đã cập nhật thêm

## Vấn đề gốc

Frontend gửi `company` thay vì `deliveryCompany` → FluentValidation báo `The DeliveryCompany field is required` khi **bàn giao vận chuyển**.

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ `DeliveryCompany` chỉ required trong `HandOverToCourierCommandValidator`. Create/Confirm/Prepare **không** yêu cầu |
| Frontend | ✅ Hand-over modal gửi đúng `deliveryCompany`; validate phía client |
| Database | ✅ `OrderShipments.DeliveryCompany` nullable |
| API | `POST /api/orders/{id}/hand-over` — bắt buộc DeliveryCompany |
| Lỗi hiện tại | Chưa tách rõ bộ request DTO theo action như roadmap; chưa hỗ trợ self-delivery “không cần đơn vị” rõ ràng |
| Việc tiếp theo | Tách request records; optional company khi giao tận tay |

## Quy tắc (roadmap vs hiện tại)

| Hành động | DeliveryCompany (roadmap) | Hiện tại |
| --------- | ------------------------- | -------- |
| Tạo đơn | Không bắt buộc | ✅ Không |
| Xác nhận | Không bắt buộc | ✅ Không |
| Chuẩn bị hàng | Không bắt buộc | ✅ Không |
| Bàn giao vận chuyển | Bắt buộc | ✅ Bắt buộc |

## Đã làm

- [x] Sửa payload FE (`deliveryCompany`, `deliveryPersonPhone`, …)
- [x] Validate FE trước khi gọi API
- [x] Create/Confirm không còn dính lỗi DeliveryCompany

## Cần cập nhật thêm (chuyên nghiệp)

- [ ] Tách rõ: `CreateOrderRequest`, `ConfirmOrderRequest`, `PrepareOrderRequest`, `UpdateShippingRequest` / `HandOverRequest`
- [ ] Cho phép phương thức “Tự giao / Gặp mặt” → `DeliveryCompany` optional hoặc giá trị cố định `SelfDelivery`
- [ ] Dropdown đơn vị vận chuyển phổ biến VN (GHN, GHTK, Viettel Post, …) + ô khác
- [ ] Message lỗi tiếng Việt thống nhất

## Hoàn thành khi

Người mua tạo đơn và người bán xác nhận **không** gặp lỗi DeliveryCompany — ✅ đã đạt.
