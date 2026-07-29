# PASSDO-02 — Chuẩn hóa tài khoản và quyền người dùng

## Trạng thái

- [x] Đã làm (cốt lõi)
- [x] Đã cập nhật thêm (dọn legacy schema + ownership tests + docs)

**Branch:** `17-refactor/PASSDO-02-remove-legacy-shipper-user-permissions`

## Quyết định nghiệp vụ (đã khớp source)

- Hệ thống chỉ có role `Admin` và `User` (`Roles.User` / `Roles.Admin`, enum `UserRole`).
- Không còn role `Buyer`, `Seller`, `Shipper`.
- Một tài khoản `User` vừa mua vừa bán — không cần đổi role.
- `BuyerId` / `SellerId` trên đơn hàng / sản phẩm là **vai trò giao dịch** (ownership), không phải role đăng nhập.
- Không tin Frontend truyền `UserId` / `SellerId` / `BuyerId` để bypass quyền — luôn lấy user hiện tại từ authenticated claims.
- Admin khóa tài khoản (`IsActive`) thuộc **PASSDO-19** — không triển khai trong issue này.

## Hiện trạng (kiểm tra source)

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend roles | ✅ Chỉ `User` / `Admin`. Migration `RemoveShipperAddChatAndHandover` đã convert `Shipper` → `User`. Không còn Shipper API / policy |
| Frontend | ✅ Đăng ký không chọn Buyer/Seller. Type `UserRole = 'User' \| 'Admin'`. Có `/purchases`, `/sales` |
| Ownership đơn hàng | ✅ `OrderActionCommands` dùng claims + `EnsureBuyer` / `EnsureSeller` → `ForbiddenException` (403). `GetOrderById` kiểm tra participant |
| Tự mua sản phẩm mình | ✅ `CreateOrder` chặn khi `product.SellerId == buyerId` (đã có unit test) |
| Seed | ✅ Chỉ seed Admin + category + sample products (SellerId = Admin). Không seed Shipper / Buyer / Seller role |
| Database schema | ✅ Đã xóa property legacy; migration `RemoveLegacyShipperFields` drop `Order.ShipperId`, `OrderShipment.ShipperId`, `OrderShipment.ShipperReceivedAt` |
| Docs | ✅ README ghi rõ 1 User = mua + bán; không còn role Shipper |
| Ownership tests | ✅ `OrderOwnershipTests`: User A không confirm / cancel / xem đơn của User B → `ForbiddenException` |

## Đã làm

- [x] Xóa role Shipper khỏi domain / auth / seed
- [x] Không còn màn, menu, route, API dành riêng cho Shipper
- [x] Đăng ký mặc định `User` — không chọn Buyer/Seller/Shipper
- [x] User có Đơn mua / Đơn bán / đăng sản phẩm
- [x] Chặn mua sản phẩm của chính mình
- [x] Order actions kiểm tra ownership theo BuyerId/SellerId từ claims
- [x] Xóa property legacy khỏi entity `Order` / `OrderShipment`
- [x] Migration mới `RemoveLegacyShipperFields` (không sửa migration cũ, không drop database)
- [x] Cập nhật README: một User vừa mua vừa bán; không còn role Shipper
- [x] Unit test ownership: User A không confirm / thao tác đơn của User B → `ForbiddenException`

## Ngoài phạm vi PASSDO-02

- [ ] Admin UI khóa tài khoản (`IsActive`) → **PASSDO-19**
- [ ] Workflow vận chuyển / tích hợp đơn vị giao hàng → **PASSDO-21** (không tạo lại role Shipper)

## Acceptance Criteria

- [x] Không còn role Shipper trong Backend và Frontend
- [x] Không còn trang / route / API dành riêng cho Shipper
- [x] Đăng ký không yêu cầu chọn Buyer/Seller
- [x] Một User vừa đăng bán vừa mua hàng
- [x] User không thể mua sản phẩm của chính mình
- [x] Đã xóa `ShipperId` và `ShipperReceivedAt` khỏi code + có migration drop column
- [x] Không sửa migration cũ và không drop database
- [x] Seed không còn Shipper
- [x] README/docs cập nhật đủ (1 account = mua + bán)
- [x] Có test authorization/ownership đơn hàng (User A ≠ User B)
- [x] Backend và Frontend build thành công; test pass

## Migration

- `20260729061211_RemoveLegacyShipperFields` — drop cột legacy trên `Orders` / `OrderShipments`.

## Hoàn thành khi

Một tài khoản đăng bán và mua sản phẩm người khác **không cần đổi role** — ✅ đã đạt.

Dọn schema legacy + ownership tests + docs — ✅ đã đạt trên branch trên.
