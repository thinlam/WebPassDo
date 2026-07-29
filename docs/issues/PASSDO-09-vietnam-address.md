# PASSDO-09 — Hoàn thiện địa chỉ Việt Nam

## Trạng thái

- [x] Đã làm (cốt lõi)
- [ ] Đã cập nhật thêm

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ LocationsController proxy open-api.vn + cache 24h; UserAddress có name + code |
| Frontend | ✅ `VietnamAddressFields` cascade Province→District→Ward, reset khi đổi cha |
| Database | ✅ Province/District/Ward + Code trên UserAddress; Order snapshot tên địa chỉ |
| API | `/api/locations/provinces|districts|wards`, `/api/me/addresses` |
| Lỗi hiện tại | Phụ thuộc API ngoài (fail → list rỗng); Order snapshot chưa lưu đủ `*Code` |
| Việc tiếp theo | Fallback dataset; snapshot code vào Order |

## Checklist dữ liệu

- [x] Tỉnh/TP
- [x] Quận/Huyện
- [x] Phường/Xã
- [x] Địa chỉ chi tiết
- [x] UI cascade + clear con khi đổi cha
- [x] Lưu ProvinceCode / DistrictCode / WardCode trên address user
- [ ] Order lưu bản sao **kèm code** tại thời điểm đặt (hiện snapshot name/street)

## Cần cập nhật thêm (chuyên nghiệp)

- [ ] Seed/local JSON fallback khi open-api.vn down
- [ ] Thêm ShippingProvinceCode / DistrictCode / WardCode trên Order
- [ ] Validate code khớp name khi create order
- [ ] Dùng province picker trên bộ lọc Home (PASSDO-11) thay free-text location
- [ ] Cập nhật theo đơn vị hành chính 2 cấp nếu VN đổi cấu trúc (ghi chú rủi ro)

## Hoàn thành khi

Cascade hoạt động + lưu address — ✅ cốt lõi; snapshot code + fallback còn thiếu.
