# PASSDO-02 — Chuẩn hóa tài khoản và quyền người dùng

## Trạng thái

- [x] Đã làm (cốt lõi)
- [ ] Đã cập nhật thêm

## Quyết định nghiệp vụ (đã khớp source)

Hệ thống chỉ có `Admin` và `User`. Một `User` vừa mua vừa bán. Không còn Buyer / Seller / Shipper role.

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ `Roles.User` / `Roles.Admin`. Migration đã convert Shipper → User. Không còn Shipper API |
| Frontend | ✅ Không chọn Buyer/Seller khi đăng ký. Có `/purchases` và `/sales` |
| Database | ✅ Role string User/Admin. ⚠️ Còn cột legacy `ShipperId` trên Order/Shipment |
| API | Auth chuẩn; order actions kiểm tra buyer/seller theo ownership, không theo role Shipper |
| Lỗi hiện tại | Cột chết `ShipperId` gây nhầm khi đọc schema |
| Việc tiếp theo | Migration dọn cột legacy; document rõ 1 account = mua + bán |

## Đã làm

- [x] Xóa role Shipper (migration `RemoveShipperAddChatAndHandover`)
- [x] Không còn màn/menu Shipper trên frontend
- [x] Đăng ký không chọn Buyer/Seller
- [x] Khu vực Đơn mua / Đơn bán trong tài khoản

## Cần cập nhật thêm (chuyên nghiệp)

- [ ] Drop cột `ShipperId` / `ShipperReceivedAt` (Order, OrderShipment) nếu không còn dùng
- [ ] Seed/README ghi rõ: mọi User đều có thể bán
- [ ] Admin UI quản lý khóa tài khoản (`IsActive`) — liên quan PASSDO-19
- [ ] Policy test: User A không confirm đơn của User B

## Hoàn thành khi

Một tài khoản đăng bán và mua sản phẩm người khác **không cần đổi role** — ✅ đã đạt.
