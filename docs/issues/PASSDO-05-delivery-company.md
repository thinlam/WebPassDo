# PASSDO-05 — Sửa lỗi DeliveryCompany bắt buộc

## Trạng thái

- [x] Đã làm (cốt lõi — lỗi field name / required sai chỗ đã fix)
- [x] Đã cập nhật thêm (dropdown VN + message VI)

## Vấn đề gốc

Frontend gửi `company` thay vì `deliveryCompany` → FluentValidation báo `The DeliveryCompany field is required` khi **bàn giao vận chuyển**. Create/Confirm đôi khi cũng dính lỗi nếu payload/validator không tách đúng.

## Hiện trạng (source)

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ `DeliveryCompany` chỉ required trong `HandOverToCourierCommandValidator` (message tiếng Việt). Create / Confirm / Prepare **không** yêu cầu |
| Frontend | ✅ Hand-over: dropdown đơn vị VN + “Khác”; validate phía client; gửi `deliveryCompany` |
| Database | ✅ `OrderShipments.DeliveryCompany` nullable (`MaxLength` 150) |
| API | `POST /api/orders/{id}/hand-over` — body `HandOverRequest`, bắt buộc `DeliveryCompany` |
| Request DTO | ✅ Đã tách theo action: `CreateOrderRequest`, confirm dùng `NoteRequest`, prepare không body, `HandOverRequest` |
| Self-delivery | → **PASSDO-21** (tự giao / gặp mặt / optional company) |

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
- [x] Create / Confirm / Prepare không còn dính lỗi DeliveryCompany
- [x] BE: required chỉ ở hand-over; các action khác không bắt company
- [x] DB: cột nullable
- [x] Request contracts tách theo endpoint
- [x] Dropdown đơn vị VN (GHN, GHTK, J&T, Viettel Post, SPX, Vietnam Post, Ninja Van, Best Express) + ô “Khác”
- [x] Message FluentValidation hand-over tiếng Việt

## Ngoài phạm vi (PASSDO-21)

- [ ] Phương thức “Tự giao / Gặp mặt / Tự thuê shipper” → `DeliveryCompany` optional hoặc giá trị cố định (vd. `SelfDelivery`)

## Liên quan

- **PASSDO-21** — hình thức VC, tự giao, tích hợp carrier, tracking.
- **PASSDO-04** — order status / bàn giao Shipping.

## File chính

| Layer | Path |
| ----- | ---- |
| FE constants | `frontend/src/lib/deliveryCompanies.ts` |
| FE modal | `frontend/src/pages/OrderDetailPage.tsx` |
| BE validator | `backend/.../OrderActions/OrderActionCommands.cs` (`HandOverToCourierCommandValidator`) |

## Hoàn thành khi

| Tiêu chí | Trạng thái |
| -------- | ---------- |
| Buyer tạo đơn / seller xác nhận **không** gặp lỗi DeliveryCompany | ✅ Đạt |
| Hand-over bắt buộc đơn vị VC đúng field | ✅ Đạt |
| Dropdown VN + message VI | ✅ Đạt |
| Self-delivery optional company | → PASSDO-21 |
