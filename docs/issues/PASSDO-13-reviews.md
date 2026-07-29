# PASSDO-13 — Đánh giá sau giao dịch

## Trạng thái

- [ ] Đã làm
- [ ] Đã cập nhật thêm

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ❌ Không có entity/API Review |
| Frontend | ❌ Không có form/trang đánh giá |
| Database | ❌ Thiếu bảng Reviews |
| API | — |
| Lỗi hiện tại | Không đánh giá được sau Completed/Delivered |
| Việc tiếp theo | Thiết kế schema + API + UI; phụ thuộc PASSDO-04 có `Completed` |

## Cần xây dựng

- [ ] Chỉ đánh giá khi order `Completed`
- [ ] Điểm tổng + đúng mô tả + đóng gói + giao tiếp + comment + ảnh
- [ ] 1 lần / order; gắn `OrderId`; chỉ buyer
- [ ] Seller phản hồi
- [ ] Admin ẩn đánh giá khi vi phạm (PASSDO-19)

## Definition of Done

**Chưa làm.**
