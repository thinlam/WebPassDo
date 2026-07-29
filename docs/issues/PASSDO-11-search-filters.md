# PASSDO-11 — Tìm kiếm và bộ lọc sản phẩm

## Trạng thái

- [ ] Đã làm
- [ ] Đã cập nhật thêm

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ GetProducts hỗ trợ filter query + phân trang |
| Frontend | ⚠️ Keyword, category, condition, location text. ❌ min/max price UI, sort UI, province picker |
| Database | ✅ Index theo nhu cầu hiện tại |
| API | `GET /api/products?keyword&categoryId&condition&minPrice&maxPrice&page…` |
| Lỗi hiện tại | FE chưa expose đủ filter dù API có; location free-text |
| Việc tiếp theo | UI filter/sort + giữ query trên URL |

## Checklist

### Bộ lọc

- [x] Từ khóa
- [x] Danh mục
- [ ] Khoảng giá (UI)
- [x] Tình trạng
- [ ] Tỉnh/TP (structured)
- [ ] Có hóa đơn / còn bảo hành (chưa có field SP)
- [ ] Phương thức giao hàng

### Sắp xếp

- [ ] Mới nhất (UI — hiện hardcode createdAt desc)
- [ ] Giá tăng / giảm
- [ ] Yêu thích nhiều

### Kỹ thuật

- [x] Phân trang backend
- [x] Filter qua query params
- [x] Không tải full rồi lọc FE
- [ ] Giữ filter khi đổi trang (URL search params)

## Definition of Done

Chưa đạt — **chưa hoàn thành**.
