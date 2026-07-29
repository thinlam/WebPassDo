# PASSDO-12 — Dark mode và responsive

## Trạng thái

- [x] Đã làm (cốt lõi)
- [ ] Đã cập nhật thêm

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | N/A (UI) |
| Frontend | ✅ Theme store light/dark/system, ThemeMenu, CSS variables, fallback map `bg-white` |
| Database | N/A (persist localStorage) |
| API | N/A |
| Lỗi hiện tại | Một số panel legacy; chưa audit đủ mọi UI state trên mọi trang |
| Việc tiếp theo | Audit trang admin/modal; skeleton; focus ring |

## Phạm vi

- [x] Trang chủ, auth, products, orders, notifications, profile (cơ bản)
- [ ] Admin + mọi modal/dropdown (cần audit)
- [x] Responsive breakpoints cơ bản
- [x] Theme lưu sau reload

## UI states bắt buộc

- [x] Loading / Empty / Error (nhiều trang dùng Spinner/EmptyState)
- [ ] Success toast thống nhất
- [ ] Disabled / Skeleton đầy đủ
- [ ] Unauthorized / Not found trang riêng rõ ràng
- [ ] Focus ring rõ trên form/nút

## Cần cập nhật thêm (chuyên nghiệp)

- [ ] Loại bỏ hard-code `bg-white` — dùng token theme thuần
- [ ] Kiểm tra không tràn mobile trên OrderDetail / Checkout / Chat
- [ ] Contrast AA cho text phụ trong dark mode
- [ ] Prefers-reduced-motion

## Hoàn thành khi

Dark mode không nền trắng lạc; mobile không tràn; theme persist — **cốt lõi ✅**, polish còn lại.
