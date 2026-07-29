# PASSDO-19 — Trang quản trị

## Trạng thái

- [ ] Đã làm
- [ ] Đã cập nhật thêm

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ⚠️ Admin rải rác (Categories CRUD, reject product, bypass order). ❌ Không AdminController tổng, ❌ không AuditLog |
| Frontend | ⚠️ Chỉ `/admin/categories` |
| Database | ❌ Thiếu AuditLogs |
| API | Categories admin-only; không user ban / dispute queue |
| Việc tiếp theo | Admin shell + modules + audit trail |

## Checklist quản trị

- [ ] Người dùng (khóa/mở)
- [ ] Sản phẩm (duyệt/ẩn/từ chối)
- [ ] Đơn hàng (overview)
- [ ] Báo cáo vi phạm
- [ ] Tranh chấp
- [x] Danh mục (đã có)
- [ ] Đánh giá (ẩn)
- [ ] Tài khoản bị khóa
- [ ] Mọi thao tác ghi AuditLog (AdminId, Action, EntityType, EntityId, Reason, Old/New, CreatedAt)

## Definition of Done

**Chưa hoàn thành** (chỉ categories).
