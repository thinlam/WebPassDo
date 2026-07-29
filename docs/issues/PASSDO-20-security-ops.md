# PASSDO-20 — Bảo mật và vận hành

## Trạng thái

- [ ] Đã làm
- [ ] Đã cập nhật thêm

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ Exception middleware, health, CORS config, JWT. ⚠️ File upload content-type only. ❌ Rate limiting, login lockout |
| Frontend | ⚠️ Token trong store; 401 → logout. ❌ Không refresh retry; tránh log secret |
| Docker | ✅ Compose + volume DB + healthcheck + env secrets |
| Database | ✅ Backup cần quy trình ops (chưa document runbook đầy đủ trong issue này) |
| Việc tiếp theo | Rate limit, magic-byte upload, tách config Dev/Prod rõ hơn |

## Checklist Backend

- [ ] Rate limiting
- [x] Kiểm tra loại file (content-type) + size
- [ ] Magic-byte / không tin filename client
- [x] Authorization ownership
- [x] Global exception handling
- [ ] Structured logging đầy đủ (Serilog/enrichers)
- [x] Health check
- [ ] Backup DB runbook
- [x] CORS theo domain config
- [x] Secret từ env (không commit .env)

## Checklist Frontend

- [x] Không render HTML user tùy ý (React text)
- [ ] Xử lý token an toàn hơn + refresh
- [x] Session hết hạn → login
- [x] Chống double submit (một phần)
- [ ] Không log thông tin nhạy cảm

## Checklist Docker

- [x] Env-based config
- [x] Volume DB
- [x] Healthcheck
- [ ] Tách compose override Dev/Prod chuyên nghiệp hơn

## Definition of Done

**Partial — chưa hoàn thành** theo DoD roadmap.
