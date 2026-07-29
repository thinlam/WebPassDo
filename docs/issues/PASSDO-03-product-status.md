# PASSDO-03 — Chuẩn hóa trạng thái sản phẩm

## Trạng thái

- [ ] Đã làm
- [ ] Đã cập nhật thêm

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
| Backend | ⚠️ Enum: `Draft, Available, Reserved, Sold, Hidden, Rejected` — **không có** `PendingReview`; dùng `Available` thay `Active` |
| Frontend | ⚠️ Mirror enum backend (`Available`). Label VN ở detail; my-products còn hiện raw status |
| Database | ✅ Cột Status (int/enum). Không cần bảng mới, cần migration rename/map nếu đổi tên |
| API | `PATCH /api/products/{id}/status`; order flow set Reserved/Sold |
| Lỗi hiện tại | Lệch naming roadmap; chưa có queue duyệt; seller có thể tự publish `Available` |
| Việc tiếp theo | Align enum + FE labels + moderation rules |

## Checklist nghiệp vụ

- [ ] Chỉ `Active` (hoặc map `Available`) mới được đặt mua — hiện dùng `Available`
- [x] Có đơn hợp lệ → `Reserved` (đã có trong create order)
- [x] Giao dịch xong → `Sold` (cần xác nhận đúng lúc Completed/Delivered)
- [x] Hủy đơn → về `Available`/`Active`
- [ ] Không tạo nhiều đơn active cùng 1 sản phẩm (kiểm tra/siết constraint)
- [ ] Thêm `PendingReview` + admin duyệt (nếu giữ quy tắc kiểm duyệt)

## Cần làm

1. Quyết định: rename `Available` → `Active` **hoặc** giữ `Available` và cập nhật roadmap docs.
2. Thêm `PendingReview` nếu cần moderation; mặc định create → PendingReview hoặc Draft.
3. Shared constants FE/BE (TypeScript + C# enum đồng bộ).
4. Localize status trên mọi màn (My Products, Admin).

## Definition of Done

Frontend và Backend cùng một bộ trạng thái, không dùng chuỗi tùy ý — **chưa đạt** (còn lệch tên / thiếu PendingReview).
