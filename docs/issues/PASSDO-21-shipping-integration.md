# PASSDO-21 — Tích hợp vận chuyển, theo dõi shipper và xác nhận giao hàng

## Trạng thái

- [ ] Đã làm
- [ ] Đã cập nhật thêm

## Liên quan roadmap

- Mở rộng / thay thế một phần: PASSDO-04 (order status), PASSDO-05 (DeliveryCompany), PASSDO-06 (seller confirm), PASSDO-07 (notifications), PASSDO-18 (disputes)
- **Không** tạo role `Shipper` (khớp PASSDO-02)

## Hiện trạng source (baseline)

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | Hand-over thủ công (`POST .../hand-over`), `OrderShipment`, status `AwaitingHandover` → `Shipping` → `Delivered`. Chưa có webhook, Adapter carrier, DeliveryProof, auto-complete |
| Frontend | Modal bàn giao nhập tên/SĐT/công ty; buyer confirm delivered. Chưa timeline carrier / map / proof |
| Database | `OrderShipments` đơn giản. Thiếu `ShippingOrder`, `ShippingDriver`, tracking history, webhook log, delivery proof |
| API | Chưa có `/shipping/*` và `/webhooks/shipping/{provider}` |

---

## Mục tiêu

Xây dựng quy trình vận chuyển minh bạch: chuẩn bị hàng → bàn giao đơn vị VC / tự giao → theo dõi hành trình → người mua xác nhận đã nhận.

Hệ thống **không** tự quản lý đội shipper và **không** cần role `Shipper`. Dữ liệu lấy từ đơn vị vận chuyển tích hợp hoặc do người bán nhập khi tự giao.

---

## Phạm vi

### Chọn hình thức vận chuyển (khi seller xác nhận / chuẩn bị)

- [ ] Đơn vị vận chuyển tích hợp
- [ ] Người bán tự giao hàng
- [ ] Người bán tự thuê shipper bên ngoài
- [ ] Người mua đến nhận trực tiếp

### Đơn vị vận chuyển tích hợp

- [ ] Tạo vận đơn + mã vận đơn
- [ ] Lưu provider, phí, ETA
- [ ] Webhook cập nhật trạng thái
- [ ] Timeline hành trình + tracking URL
- [ ] Thông tin shipper / bằng chứng giao (nếu API có)
- [ ] Thiết kế **Adapter** (GHN, GHTK, SPX, Ahamove, GrabExpress — phase đầu chưa bắt buộc live)

### Tự giao / tự thuê shipper

Nhập: tên, SĐT (mask `09******12`), loại phương tiện, biển số (optional), ETA, ghi chú. Chỉ hiện khi đơn đang giao. Không giả lập GPS/shipper.

### Xác nhận người mua & bảo vệ giao dịch

- [ ] Delivered từ carrier → **Chờ buyer confirm**, không auto `Completed`
- [ ] Buyer: đã nhận / chưa nhận / sai mô tả / hư hỏng / báo sự cố
- [ ] Auto-complete sau N giờ (config, không hard-code)
- [ ] Khiếu nại → pause complete + pause giải ngân

---

## Luồng trạng thái đề xuất

```text
Chờ người bán xác nhận
→ Đang chuẩn bị hàng
→ Chờ đơn vị vận chuyển lấy hàng
→ Đơn vị vận chuyển đã nhận hàng
→ Đang vận chuyển
→ Đang giao đến người nhận
→ Đã giao - Chờ người mua xác nhận
→ Hoàn tất
```

**Ngoại lệ:** Giao không thành công · Chờ giao lại · Buyer từ chối nhận · Đang hoàn · Đã hoàn · Thất lạc · Đang khiếu nại · Đã hủy

**Tự giao:** Chuẩn bị xong → seller bấm *Bắt đầu giao hàng* → Đang giao đến người nhận.

**Tích hợp carrier:** Không cho seller tự xác nhận đã bàn giao khi đã gắn API — chỉ webhook.

---

## Entity đề xuất

`ShippingOrder` · `ShippingDriver` · `ShippingTrackingHistory` · `DeliveryProof` · `ShippingWebhookLog`

## API đề xuất

```http
POST /api/orders/{orderId}/shipping
POST /api/orders/{orderId}/shipping/self-delivery
GET  /api/orders/{orderId}/shipping
GET  /api/orders/{orderId}/shipping/tracking
GET  /api/orders/{orderId}/shipping/proof
POST /api/orders/{orderId}/shipping/start-delivery
POST /api/orders/{orderId}/confirm-received
POST /api/orders/{orderId}/report-delivery-issue
POST /api/webhooks/shipping/{provider}
```

---

## Phases

| Phase | Nội dung |
| ----- | -------- |
| 1 | Entity/status, tạo vận đơn, mã VC, webhook, timeline, ETA |
| 2 | Shipper info, ETA/map/link, proof, OTP/ký, buyer confirm |
| 3 | Auto-complete, hold payout, khiếu nại/hoàn, webhook đối soát, admin config provider |

---

## Acceptance Criteria

- [ ] Seller chọn hình thức VC
- [ ] Lưu provider + mã vận đơn
- [ ] Webhook cập nhật status (verify + chống trùng + không đi lùi)
- [ ] Buyer xem timeline + ETA
- [ ] Hiện shipper chỉ khi provider có data; ẩn sau hoàn tất/hủy
- [ ] Delivered ≠ Completed; buyer confirm hoặc auto-complete theo config
- [ ] Báo sự cố / khiếu nại pause complete + payout
- [ ] Lưu proof nếu có; self-delivery upload chỉ tham khảo
- [ ] Realtime noti đủ sự kiện VC
- [ ] Hỗ trợ tự giao; **không** tạo role Shipper

## Lưu ý

Không fake GPS/shipper · Không giữ PII shipper lâu hơn cần · Không giải ngân khi mới Delivered · Adapter đa provider.

## Việc tiếp theo

1. Align enum OrderStatus với luồng trên (PASSDO-04)
2. Migration entity shipping
3. Adapter interface + stub provider (dev) trước GHN live
4. FE seller/buyer shipping panels
5. Background job auto-complete
