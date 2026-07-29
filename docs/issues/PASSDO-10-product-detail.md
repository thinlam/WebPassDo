# PASSDO-10 — Hoàn thiện trang chi tiết sản phẩm

## Trạng thái

- [ ] Đã làm
- [ ] Đã cập nhật thêm

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ Product detail + images + seller fields cơ bản. ❌ Defects, report, transparency fields |
| Frontend | ✅ Gallery, giá, mô tả, condition, seller card, mua/chat/favorite, owner panel. ❌ Report, defects, related orders |
| Database | ✅ Products + ProductImages. ❌ Defects / reports |
| API | `GET /api/products/{id}`, favorites, chat create |
| Lỗi hiện tại | Thiếu nhiều mục roadmap; My Products thiếu mark sold / related orders |
| Việc tiếp theo | Bổ sung UI + API theo checklist |

## Checklist hiển thị chi tiết

- [x] Bộ ảnh
- [x] Tên, giá, tình trạng, mô tả
- [ ] Khuyết điểm (nổi bật)
- [x] Thông tin người bán (cơ bản)
- [x] Khu vực / ngày đăng
- [x] Phương thức giao nhận (một phần qua checkout)
- [x] Trạng thái còn hàng (Available)
- [x] Nút mua / nhắn tin / yêu thích
- [ ] Nút báo cáo

## Đồ của tôi

- [x] Xem chi tiết / chỉnh sửa / tạm ẩn
- [ ] Đăng lại (riêng biệt, gồm Rejected → Active)
- [ ] Đánh dấu đã bán (manual)
- [ ] Xem đơn hàng liên quan
- [x] Không sửa SP người khác (API ownership)
- [ ] Chặn xóa khi đang có đơn active (verify)

## Definition of Done

Chưa đủ checklist — **chưa hoàn thành**.
