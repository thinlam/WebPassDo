# PASSDO-16 — Minh bạch giá và lịch sử giá

## Trạng thái

- [ ] Đã làm
- [ ] Đã cập nhật thêm

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ⚠️ Có OriginalPrice/SellingPrice; checkout preview phí ship. ❌ Không có PriceHistory |
| Frontend | ⚠️ Hiện giá + preview tổng ở checkout. ❌ Lịch sử giá |
| Database | ❌ Thiếu bảng PriceHistory |
| API | Preview order có tổng tiền |
| Việc tiếp theo | Audit trail khi đổi giá; không tạo % giảm giả |

## Checklist

- [x] Hiện giá SP
- [x] Phí giao dự kiến (preview)
- [ ] Phí nền tảng (nếu có)
- [x] Tổng trước khi confirm đặt hàng
- [ ] Lưu OldPrice/NewPrice/ChangedAt/ChangedBy — không xóa lịch sử

## Definition of Done

**Chưa làm** (lịch sử giá). Sprint 3.
