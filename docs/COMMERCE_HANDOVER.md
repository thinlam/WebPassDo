# PassDo Commerce Handover

## Seeded accounts
- Admin: `admin@passdo.local` / `Admin@123456`
- Shipper: `shipper@passdo.local` / `Shipper@123456`

## New / changed entities
- UserAddress, UserBankAccount
- OrderItem, OrderPayment, OrderShipment, OrderStatusHistory
- Product: Quantity, PickupAddressId, BankAccountId, AcceptedPaymentOption, AllowedDeliverySpeeds
- Order: OrderCode, totals, payment/shipping snapshots, ETA, timestamps, ShipperId
- UserRole.Shipper
- OrderStatus: AwaitingPayment → PendingConfirmation → AwaitingPickup → Shipping → Delivered (+ Cancelled, DeliveryFailed, Returned, Refunded)

## Migration
- `20260723174221_CommerceAndShipping` (maps legacy Pending/Accepted/Completed/Rejected)

## Key APIs
### Settings
- GET/POST/PUT/DELETE `/api/me/addresses`, PUT `.../default`
- GET/POST/PUT/DELETE `/api/me/bank-accounts`, PUT `.../default`

### Products (extended)
- POST/PUT `/api/products` — quantity, pickup, bank, payment option, delivery speeds
- Price/delete blocked when active orders exist

### Orders
- POST `/api/orders/preview`
- POST `/api/orders`
- GET `/api/orders/my-purchases?status=`
- GET `/api/orders/my-sales?status=`
- GET `/api/orders/shipper?availableOnly=`
- GET `/api/orders/{id}`
- POST `.../payment-proof`, `confirm-payment`, `confirm`, `reject`, `cancel`, `mark-prepared`, `assign-shipper`, `claim`, `confirm-pickup`, `confirm-delivered`, `fail-delivery`

## Frontend routes
- `/settings`, `/products/:id/edit`, `/checkout/:productId`, `/orders/:id`, `/shipper/orders`
- Purchases/Sales tabs + Vietnamese status labels

## Config
`appsettings.json` → `Shipping.Eta` + `Shipping.Fees` (ETA from shipper pickup time)

## Out of scope
- Real carrier APIs / VNPay-MoMo
- Multi-item cart
- Full return/refund workflow UI
