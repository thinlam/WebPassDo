# PASSDO-01 — Kiểm tra trạng thái source hiện tại

## Trạng thái

- [x] Đã làm
- [x] Đã cập nhật thêm (tài liệu audit + tracker Excel)

## Mục tiêu

Audit Backend, Frontend, Database để xác định phần đã làm / UI-only / API chưa dùng / mock / lỗi / migration thiếu.

## Kết quả kiểm tra

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ Build thành công (`dotnet build PassDo.sln`) |
| Frontend | ⚠️ Build fail nếu chưa `npm install` (thiếu `@react-oauth/google`) |
| Database | ✅ 6 migrations, schema commerce + chat + notifications |
| API | ✅ Swagger `/swagger` khi bật Development / `Swagger:Enabled` |
| Lỗi hiện tại | Frontend deps chưa cài trên máy audit; một số enum/status lệch roadmap |
| Việc tiếp theo | Giữ file này + `passdo-current-status.md` cập nhật mỗi sprint |

## Deliverables

- [x] `docs/issues/passdo-current-status.md`
- [x] `docs/issues/PASSDO-01` … `PASSDO-20` issue docs
- [x] `docs/issues/PassDo-Roadmap-Tracker.xlsx`

## Cần cập nhật thêm (chuyên nghiệp)

- [ ] Chạy lại audit sau mỗi sprint (cập nhật cột trạng thái + Excel)
- [ ] Gắn link PR/commit vào từng issue khi đóng task
- [ ] Thêm cột “Owner / ETA” khi team > 1 người
- [ ] CI check: backend build + frontend build + migrate smoke

## Definition of Done (PASSDO-01)

- [x] Backend build thành công
- [ ] Frontend build thành công trên máy sạch (`npm ci && npm run build`)
- [x] Docker có cấu hình chạy được (compose + healthcheck)
- [x] Swagger hoạt động (khi bật)
- [x] Có danh sách rõ phần đã làm / chưa làm
