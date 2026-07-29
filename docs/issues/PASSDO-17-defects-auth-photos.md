# PASSDO-17 — Ảnh xác thực và khuyết điểm sản phẩm

## Trạng thái

- [ ] Đã làm
- [ ] Đã cập nhật thêm

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ⚠️ ProductImage generic (primary/order). ❌ Không phân loại ảnh / defects |
| Frontend | ⚠️ Gallery thường. ❌ Section khuyết điểm |
| Database | ❌ Thiếu Defects + ImageType |
| API | Upload image hiện tại không có type |
| Việc tiếp theo | ImageType enum + Defect entity + UI nổi bật |

## Cần xây

- [ ] Phân loại: sản phẩm / chụp trực tiếp / khuyết điểm / hóa đơn / serial
- [ ] Khuyết điểm: trầy, rách, móp, ố, mòn, nứt, thiếu linh kiện
- [ ] Detail: block “Sản phẩm có các điểm cần lưu ý” + mô tả + ảnh

## Definition of Done

**Chưa làm.** Sprint 3.
