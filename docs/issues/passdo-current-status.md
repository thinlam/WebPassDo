# PassDo — Current Status Audit (PASSDO-01)

> Ngày audit: **2026-07-29** (cập nhật PASSDO-03)  
> Nguồn: Backend (`PassDo.sln`), Frontend (`frontend/`), Docker, Migrations.

## Kết quả kiểm tra môi trường

| Hạng mục | Kết quả | Ghi chú |
| -------- | ------- | ------- |
| Backend build | ✅ Thành công | `dotnet build PassDo.sln` — 0 error |
| Frontend build | ⚠️ Fail nếu thiếu deps | Thiếu `node_modules/@react-oauth/google` — chạy `npm install` rồi build lại |
| Docker | ✅ Có cấu hình | `docker-compose.yml` + healthcheck db/backend/frontend |
| Swagger | ✅ Có | `/swagger` khi Development hoặc `Swagger:Enabled=true` |
| Dữ liệu giả (mock) | ✅ Không dùng | Frontend gọi API thật qua `features/*/api.ts` |

## Tóm tắt theo giai đoạn

| ID | Task | Trạng thái | Đã làm | Cần cập nhật thêm |
| -- | ---- | ---------- | ------ | ----------------- |
| PASSDO-01 | Audit source hiện tại | ✅ Done | [x] | [x] (tài liệu này) |
| PASSDO-02 | Chuẩn hóa tài khoản & quyền | ✅ Done | [x] | [x] migration `RemoveLegacyShipperFields` + ownership tests |
| PASSDO-03 | Chuẩn hóa trạng thái sản phẩm | ✅ Done (cốt lõi) | [x] | [ ] admin queue UI + multi-stock deferred |
| PASSDO-04 | Chuẩn hóa trạng thái đơn hàng | ⬜ Chưa đạt roadmap | [ ] | [ ] |
| PASSDO-05 | Fix DeliveryCompany | ✅ Done (cốt lõi) | [x] | [ ] tách DTO / self-delivery |
| PASSDO-06 | Người bán xác nhận đơn | ✅ Done (cốt lõi) | [x] | [ ] lý do từ chối chuẩn hóa |
| PASSDO-07 | Hệ thống thông báo | ✅ Done (cốt lõi) | [x] | [ ] đủ loại noti + deep-link |
| PASSDO-08 | Đăng nhập & đăng ký | ⬜ Partial | [ ] | [ ] |
| PASSDO-09 | Địa chỉ Việt Nam | ✅ Done (cốt lõi) | [x] | [ ] snapshot code + fallback |
| PASSDO-10 | Chi tiết sản phẩm | ⬜ Partial | [ ] | [ ] |
| PASSDO-11 | Tìm kiếm & bộ lọc | ⬜ Partial | [ ] | [ ] |
| PASSDO-12 | Dark mode & responsive | ✅ Done (cốt lõi) | [x] | [ ] audit UI state đầy đủ |
| PASSDO-13 | Đánh giá sau giao dịch | ⬜ Chưa làm | [ ] | [ ] |
| PASSDO-14 | Hồ sơ uy tín người bán | ⬜ Chưa làm | [ ] | [ ] |
| PASSDO-15 | Hồ sơ minh bạch sản phẩm | ⬜ Chưa làm | [ ] | [ ] |
| PASSDO-16 | Minh bạch giá & lịch sử giá | ⬜ Chưa làm | [ ] | [ ] |
| PASSDO-17 | Ảnh xác thực & khuyết điểm | ⬜ Chưa làm | [ ] | [ ] |
| PASSDO-18 | Khiếu nại & bằng chứng | ⬜ Chưa làm | [ ] | [ ] |
| PASSDO-19 | Trang quản trị | ⬜ Partial (chỉ categories) | [ ] | [ ] |
| PASSDO-20 | Bảo mật & vận hành | ⬜ Partial | [ ] | [ ] |

## Ma trận chức năng chi tiết

### Auth & Roles

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ JWT + refresh + Google login + change password. Role chỉ `User`/`Admin`. ❌ Forgot password, rate limit login, email verify riêng |
| Frontend | ✅ Login/Register, show-hide password, strength meter, Google button, Đơn mua / Đơn bán. ❌ Forgot password, Remember me, refresh token khi 401 |
| Database | ✅ `Users`, `RefreshTokens`. Role chỉ `User`/`Admin`. Đã drop cột legacy `ShipperId` / `ShipperReceivedAt` (`RemoveLegacyShipperFields`) |
| API | `POST /api/auth/register`, `/login`, `/google`, `/refresh-token`, `/logout`, `/change-password` |
| Lỗi hiện tại | Frontend build fail nếu chưa `npm install`; 401 force logout thay vì refresh |
| Việc tiếp theo | PASSDO-08 hoàn thiện UX/bảo mật auth |

### Products

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ CRUD + images + status patch. Enum: `Draft, Active, Reserved, Sold, Hidden, Rejected, PendingReview`. Transition gates + one-order constraint |
| Frontend | ✅ List, detail, create/edit, my-products. Shared VN status labels + seller actions. Partial filters |
| Database | ✅ `Products`, `ProductImages`, `Favorites`, `Categories`. Filtered unique index `UX_Orders_OneActivePerProduct` |
| API | `/api/products`, `/my-products`, `/{id}`, status, images |
| Lỗi hiện tại | Chưa có admin products page (Approve/Reject UI); multi-stock out of scope |
| Việc tiếp theo | PASSDO-10/11/15/17 nâng cấp; admin moderation UI follow-up |

### Orders

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ Create, confirm, reject, cancel, prepare, hand-over, deliver, fail. Có `OrderStatusHistory` |
| Frontend | ✅ Checkout, purchases, sales, order detail + timeline + handover modal |
| Database | ✅ `Orders`, `OrderItems`, `OrderPayments`, `OrderShipments`, `OrderStatusHistories` |
| API | `/api/orders`, `/confirm`, `/reject`, `/cancel`, `/mark-prepared`, `/hand-over`, … |
| Lỗi hiện tại | Enum lệch roadmap (thiếu `Completed`/`Disputed`); `DeliveryCompany` bắt buộc lúc hand-over (đúng nghiệp vụ bàn giao, đã fix sai tên field FE) |
| Việc tiếp theo | PASSDO-04/05/06 polish |

### Notifications

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ Entity + CRUD read + SignalR `NotificationReceived` |
| Frontend | ✅ Bell, unread, mark read, realtime bridge |
| Database | ✅ `Notifications` |
| API | `/api/notifications`, `/unread-count`, `/{id}/read`, `/read-all` |
| Lỗi hiện tại | Thiếu một số loại (reject, dispute, review); click product chưa fallback tốt; `OrderShipping` constant chưa dùng |
| Việc tiếp theo | PASSDO-07 bổ sung loại + deep-link |

### Addresses / Locations

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ Proxy `provinces.open-api.vn` + cache; address CRUD |
| Frontend | ✅ Cascading Province→District→Ward |
| Database | ✅ `UserAddresses` (+ code columns). Order snapshot tên địa chỉ khi đặt hàng |
| API | `/api/locations/*`, `/api/me/addresses` |
| Lỗi hiện tại | Phụ thuộc API ngoài, không có dataset local fallback; order snapshot chưa lưu đủ `*Code` |
| Việc tiếp theo | PASSDO-09 |

### Reviews / Disputes / Admin / Transparency

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ❌ Review, Dispute, PriceHistory, Defect, AuditLog — chưa có |
| Frontend | ❌ Tương ứng chưa có (Admin chỉ có Categories) |
| Database | ❌ Thiếu bảng tương ứng |
| Việc tiếp theo | Giai đoạn 2–4 theo roadmap |

## Database — migrations hiện có

1. `20260722064413_InitialCreate`
2. `20260723174221_CommerceAndShipping`
3. `20260724054020_RemoveShipperAddChatAndHandover`
4. `20260724093911_AddUserLastSeenAt`
5. `20260725140710_AddGoogleAuthNotificationsAndAddressCodes`
6. `20260725142445_AddProductViewCount`
7. `20260729074415_OneActiveOrderPerProduct`

**Bảng có:** Users, Categories, Products, ProductImages, Favorites, Orders, OrderItems, OrderPayments, OrderShipments, OrderStatusHistories, UserAddresses, UserBankAccounts, RefreshTokens, Conversations, Messages, Notifications.

**Bảng thiếu (roadmap):** Reviews, Disputes/Complaints, AuditLogs, PriceHistory, ProductDefects, (tuỳ chọn) VN location seed tables.

## Ưu tiên triển khai tiếp (Sprint 1)

1. PASSDO-04 — Chuẩn hóa trạng thái đơn hàng  
2. PASSDO-05 — Polish DeliveryCompany / DTO  
3. PASSDO-06 — Lý do từ chối chuẩn hóa + noti reject  
4. PASSDO-07 — Đủ loại thông báo + deep-link  

Chi tiết từng task: xem các file `docs/issues/PASSDO-XX-*.md` và file Excel `docs/issues/PassDo-Roadmap-Tracker.xlsx`.
