# PassDo — Báo cáo nâng cấp chức năng (Auth, Địa chỉ VN, Đơn hàng, Thông báo, Dark mode)

Tài liệu này mô tả chi tiết các thay đổi đã triển khai trên source hiện tại, nguyên nhân lỗi, API mới, cách cấu hình và checklist kiểm thử.

> **Nguyên tắc:** mọi chức năng đều nối Backend và lưu dữ liệu thật (không mock). Google OAuth xác thực token phía Backend. Địa chỉ dùng API hành chính VN. Thông báo lưu Database.

---

## 1. Nguyên nhân lỗi `DeliveryCompany` (và cách sửa)

### 1.1. Root cause

Frontend gửi payload bàn giao với **tên field sai** so với Backend:


| Frontend cũ (sai)       | Backend yêu cầu (đúng)                          |
| ----------------------- | ----------------------------------------------- |
| `phone`                 | `deliveryPersonPhone`                           |
| `company`               | `deliveryCompany`                               |
| `note`                  | `deliveryNote`                                  |
| `estimatedDeliveryTime` | `estimatedDeliveryFrom` / `estimatedDeliveryTo` |


Backend bind JSON camelCase → PascalCase. Khi FE gửi `company`, property `DeliveryCompany` nhận `null`/`default` → FluentValidation báo:

```text
The DeliveryCompany field is required.
```



### 1.2. Payload cũ vs payload sau khi sửa

**Payload cũ (lỗi):**

```json
{
  "deliveryPersonName": "Lâm Nguyễn Thìn",
  "phone": "0946705264",
  "company": "Chủ shop",
  "vehicleNumber": "60A-XXX-XX",
  "trackingCode": "DH072019",
  "note": "",
  "estimatedDeliveryTime": "2026-07-26T08:28"
}
```

**Payload chuẩn sau khi sửa:**

```json
{
  "deliveryPersonName": "Lâm Nguyễn Thìn",
  "deliveryPersonPhone": "0946705264",
  "deliveryCompany": "Chủ shop",
  "vehicleNumber": "60A-XXX-XX",
  "trackingCode": "DH072019",
  "deliveryNote": "",
  "estimatedDeliveryFrom": "2026-07-26T01:28:00.000Z",
  "estimatedDeliveryTo": "2026-07-26T01:28:00.000Z"
}
```



### 1.3. File đã sửa

- `frontend/src/types/index.ts` — `HandOverPayload`, `OrderShipment`
- `frontend/src/pages/OrderDetailPage.tsx` — form state, validate từng field, không đóng modal khi API lỗi, chống double-submit, hiển thị thông tin giao hàng đúng field mới
- `backend/.../OrderActionCommands.cs` — trim trước validate/save



### 1.4. Hành vi sau khi sửa

- Nhập đủ → bàn giao thành công, modal đóng, đơn → `Shipping`, lịch sử cập nhật
- Bỏ trống Đơn vị vận chuyển → lỗi ngay dưới field (không gọi API)
- API lỗi → giữ modal, hiện lỗi trong modal
- Người mua xem được người giao / SĐT / đơn vị khi status `Shipping`

---



## 2. Authentication — hiện/ẩn mật khẩu, độ mạnh, Google OAuth



### 2.1. Hiện/ẩn mật khẩu

Component `PasswordInput` (`frontend/src/components/common/PasswordInput.tsx`):

- Mặc định `type="password"`
- Nút mắt bên phải toggle `text`/`password`
- Icon đổi theo trạng thái
- Không làm mất giá trị đang nhập (chỉ đổi `type`)

Dùng ở: Login, Register (2 ô độc lập), Account Security.

### 2.2. Đăng ký — xác nhận mật khẩu + checklist độ mạnh

Frontend:

- Schema Zod: `password === confirmPassword`
- `isPasswordStrong`: ≥8, hoa, thường, số, ký tự đặc biệt `!@#$%^&*`
- `PasswordStrengthMeter`: Yếu / Trung bình / Mạnh + checklist ✓/✕

Backend (bắt buộc validate lại):

```csharp
// Application/Common/Validation/PasswordRules.cs
RuleFor(x => x.Password).MustBeStrongPassword();
```

Áp dụng cho `RegisterCommand` và `ChangePasswordCommand`.

Password vẫn hash bằng **PBKDF2-SHA256** (100k iterations) — one-way, không giải mã ngược. `PasswordHash` không trả về bất kỳ DTO nào.

### 2.3. Google OAuth (thật, không nút giả)



#### Flow

```text
[FE] Google Identity Services → credential (ID token JWT)
        ↓
[FE] POST /api/auth/google { "idToken": "..." }
        ↓
[BE] GoogleJsonWebSignature.ValidateAsync(idToken, audience=ClientId)
        ↓
Tìm user theo GoogleSubject → hoặc Email
  - Email đã có: link GoogleSubject (không tạo trùng)
  - Chưa có: tạo user mới, PasswordHash = ""
        ↓
Trả AuthResponseDto (accessToken + refreshToken) như login thường
```



#### API

`POST /api/auth/google`

```json
{ "idToken": "<Google ID token>" }
```

Response giống `/auth/login` / `/auth/register`.

#### Cấu hình (không commit secret)

`.env` / environment:

```bash
# Backend
Google__ClientId=YOUR_GOOGLE_OAUTH_CLIENT_ID.apps.googleusercontent.com

# Frontend (public Client ID — không phải secret)
VITE_GOOGLE_CLIENT_ID=YOUR_GOOGLE_OAUTH_CLIENT_ID.apps.googleusercontent.com
```

`appsettings.json`:

```json
"Google": {
  "ClientId": ""
}
```

> SPA dùng Google ID token verification chỉ cần **Client ID**. Không cần Client Secret trong PassDo.



#### File Backend quan trọng


| File                                              | Vai trò                                                               |
| ------------------------------------------------- | --------------------------------------------------------------------- |
| `Domain/Entities/User.cs`                         | thêm `GoogleSubject`                                                  |
| `Auth/Commands/GoogleLogin/GoogleLoginCommand.cs` | tạo/link user                                                         |
| `Infrastructure/Identity/GoogleTokenValidator.cs` | verify token                                                          |
| `Api/Controllers/AuthController.cs`               | `POST google`                                                         |
| `LoginCommand.cs`                                 | login email **hoặc** SĐT; từ chối nếu PasswordHash rỗng (Google-only) |




#### File Frontend


| File                                       | Vai trò                                 |
| ------------------------------------------ | --------------------------------------- |
| `components/auth/GoogleSignInButton.tsx`   | nút Google thật (`@react-oauth/google`) |
| `pages/LoginPage.tsx` / `RegisterPage.tsx` | form + Google                           |
| `features/auth/api.ts`                     | `googleLogin(idToken)`                  |
| `layouts/MainLayout.tsx`                   | `GoogleOAuthProvider`                   |




#### Edge cases

- User đóng popup / từ chối → hiện lỗi rõ
- Email Google đã tồn tại → link, không tạo trùng
- Google-only user đăng nhập bằng mật khẩu → Backend báo lỗi rõ
- Sau đăng ký Google thiếu SĐT → điều hướng `/settings?tab=addresses` để bổ sung

---



## 3. Chuẩn hóa địa chỉ Việt Nam (combobox phụ thuộc)



### 3.1. Nguồn dữ liệu

Backend `VietnamLocationService` gọi **[provinces.open-api.vn](https://provinces.open-api.vn)** (`?depth=3`), cache memory **24 giờ**.

API PassDo (AllowAnonymous):


| Method | Route                                    | Mô tả                              |
| ------ | ---------------------------------------- | ---------------------------------- |
| GET    | `/api/locations/provinces`               | Danh sách tỉnh/TP `{ code, name }` |
| GET    | `/api/locations/districts?provinceCode=` | Quận/huyện theo tỉnh               |
| GET    | `/api/locations/wards?districtCode=`     | Phường/xã theo quận                |




### 3.2. Lưu Database

`UserAddress` bổ sung:

- `ProvinceCode`, `DistrictCode`, `WardCode` (optional)
- Giữ `Province`, `District`, `Ward` = **tên** hiển thị
- `StreetAddress` = AddressLine (số nhà, đường, tòa nhà...)



### 3.3. UI

`VietnamAddressFields`:

1. Chọn Tỉnh → load Quận
2. Đổi Tỉnh → reset Quận + Phường
3. Chọn Quận → load Phường
4. Đổi Quận → reset Phường
5. Có ô tìm kiếm trong combobox
6. Loading khi fetch
7. Không cho chọn cấp dưới khi chưa chọn cấp trên
8. Chỉ hiện **tên**, không hiện mã code
9. Sửa địa chỉ bind lại tên + code đã lưu

Áp dụng ở **Cài đặt → Địa chỉ** (form thêm/sửa). Các form chọn địa chỉ có sẵn (checkout, đăng bán) vẫn chọn từ danh sách địa chỉ đã chuẩn hóa.

---



## 4. Đồ của tôi — Xem chi tiết



### Thao tác trên mỗi sản phẩm

- **Xem chi tiết** → `/products/:id`
- **Sửa** → `/products/:id/edit`
- **Ẩn / Hiện** theo status
- **Xóa**



### Trang chi tiết (owner)

Khi người xem là chủ sản phẩm, Backend trả thêm:

- `sellerPhoneNumber`
- `pickupAddressFull`
- `bankName`, `bankAccountNumberMasked`, `bankAccountHolderName`
- `updatedAt`

Người khác **không** thấy block quản trị / tài khoản ngân hàng / nút Sửa.

Nút owner: **Sửa sản phẩm**, **Quay lại Đồ của tôi**.

---



## 5. Thông báo đơn hàng (Database + UI chuông)



### 5.1. Entity `Notification`


| Field                  | Ý nghĩa                       |
| ---------------------- | ----------------------------- |
| UserId                 | Người nhận                    |
| Type                   | NewOrder, OrderConfirmed, ... |
| Title / Content        | Tiêu đề / nội dung            |
| RelatedEntityId / Type | OrderId / `"Order"`           |
| ActionUrl              | `/orders/{id}`                |
| IsRead / ReadAt        | Trạng thái đọc                |
| CreatedAt              | Thời gian tạo                 |




### 5.2. Thời điểm tạo

Chỉ sau `SaveChanges` thành công khi tạo đơn / đổi trạng thái:


| Sự kiện              | Người nhận  |
| -------------------- | ----------- |
| Tạo đơn mới          | Người bán   |
| Xác nhận đơn         | Người mua   |
| Chuẩn bị hàng        | Người mua   |
| Bàn giao / đang giao | Người mua   |
| Xác nhận nhận hàng   | Người bán   |
| Hủy / từ chối        | Bên còn lại |


Ví dụ nội dung đơn mới:

> **Bạn có một đơn hàng mới**  
> Một khách hàng vừa đặt mua sản phẩm “Áo thun” với tổng giá trị 60.000 ₫. Đơn hàng đang chờ bạn xác nhận.



### 5.3. API


| Method | Route                                |
| ------ | ------------------------------------ |
| GET    | `/api/notifications?page=&pageSize=` |
| GET    | `/api/notifications/unread-count`    |
| POST   | `/api/notifications/{id}/read`       |
| POST   | `/api/notifications/read-all`        |




### 5.4. UI

- Chuông trên header + badge số chưa đọc
- Poll 30s
- Click → đánh dấu đã đọc → mở `/orders/{id}`
- Nút “Đánh dấu tất cả đã đọc”
- Empty state khi chưa có thông báo

---



## 6. Dark mode toàn site



### Cách lưu / khôi phục

- Zustand persist key: `passdo-theme`
- Preference: `light` | `dark` | `system`
- Áp dụng ngay qua `document.documentElement.setAttribute('data-theme', ...)`
- `system` lắng nghe `prefers-color-scheme`



### Theme tokens (`index.css`)

```css
:root, [data-theme="light"] {
  --color-paper: #f7faf8;
  --color-surface: #ffffff;
  --color-ink: #1c2a24;
  --color-muted: #5d6f66;
  --color-line: #cfdcd4;
  /* ... */
}

[data-theme="dark"] {
  --color-paper: #121714;
  --color-surface: #1b221e;
  --color-ink: #edf4ef;
  --color-muted: #aab7af;
  --color-line: #354039;
  /* không dùng #000 tuyệt đối */
}
```

UI controls dùng `bg-surface`, `text-ink`, `border-line`. Legacy `bg-white` được map soft trong dark mode.

Chuyển theme ở:

- Dropdown user (đã đăng nhập)
- Icon theme trên header (khách)

Không cần reload trang.

---



## 7. Migration đã thêm

```text
20260725140710_AddGoogleAuthNotificationsAndAddressCodes
```

Nội dung chính:

- `Users.GoogleSubject` (+ unique filtered index)
- `Notifications` table
- `UserAddresses.ProvinceCode / DistrictCode / WardCode`

Áp dụng:

```bash
cd backend
dotnet ef database update --project src/PassDo.Infrastructure --startup-project src/PassDo.Api
```

Hoặc Docker với `APPLY_MIGRATIONS=true`.

---



## 8. Danh sách file chính đã tạo / sửa



### Backend (tạo mới)

- `Auth/Commands/GoogleLogin/GoogleLoginCommand.cs`
- `Common/Validation/PasswordRules.cs`
- `Common/Helpers/MoneyFormatter.cs`
- `Locations/**`, `Notifications/**`
- `Domain/Entities/Notification.cs`
- `Domain/Constants/NotificationTypes.cs`
- `Infrastructure/Identity/GoogleTokenValidator.cs`
- `Infrastructure/Services/VietnamLocationService.cs`
- `Infrastructure/Services/NotificationService.cs`
- `Api/Controllers/LocationsController.cs`
- `Api/Controllers/NotificationsController.cs`
- Migration `AddGoogleAuthNotificationsAndAddressCodes`



### Backend (sửa)

- `User.cs`, `UserAddress.cs`, `PassDoDbContext.cs`, `IApplicationDbContext.cs`
- `RegisterCommand`, `LoginCommand`, `ChangePasswordCommand`
- `CreateOrderCommand`, `OrderActionCommands`
- `GetProductByIdQuery`, `ProductDtos`, `ProductMapper`
- `AddressCommands`, `SettingsRequests`, `AuthController`, `MeSettingsController`
- `DependencyInjection.cs`, `appsettings*.json`, `.env.example`, `docker-compose.yml`



### Frontend (tạo mới)

- `components/common/PasswordInput.tsx`
- `components/auth/PasswordStrengthMeter.tsx`, `GoogleSignInButton.tsx`
- `components/address/VietnamAddressFields.tsx`
- `components/notifications/NotificationBell.tsx`
- `components/theme/ThemeProvider.tsx`, `ThemeMenu.tsx`
- `stores/themeStore.ts`
- `features/locations/api.ts`, `features/notifications/api.ts`
- `lib/passwordStrength.ts`



### Frontend (sửa)

- `LoginPage`, `RegisterPage`, `AccountSecurityPage`
- `SettingsPage` (AddressForm)
- `MyProductsPage`, `ProductDetailPage`, `OrderDetailPage`
- `MainLayout`, `main.tsx`, `index.css`, `ui.tsx`
- `types/index.ts`, `features/auth/api.ts`, `features/addresses/api.ts`

---



## 9. Checklist kiểm thử



### Authentication

- [x] Hiện/ẩn mật khẩu login
- [x] Hiện/ẩn độc lập 2 ô mật khẩu đăng ký
- [x] Hai mật khẩu không khớp → lỗi dưới field, không gửi API
- [x] Mật khẩu yếu → Frontend + Backend từ chối
- [x] Google login thành công (cần cấu hình Client ID)
- [x] Google email đã tồn tại → không tạo trùng
- [x] Hủy popup Google → lỗi rõ ràng



### Địa chỉ

- [x] Chọn tỉnh → load đúng quận
- [x] Đổi tỉnh → reset quận + phường
- [x] Chọn quận → load đúng phường
- [x] Sửa địa chỉ bind đúng dữ liệu cũ



### Sản phẩm

- [x] Đồ của tôi có **Xem chi tiết**
- [x] Chi tiết đúng sản phẩm
- [x] Người không phải chủ không thấy nút Sửa / block quản trị
- [x] Sửa sản phẩm không màn hình trắng



### Đơn hàng

- [x] Bàn giao đủ field → thành công, không còn lỗi DeliveryCompany
- [x] Bỏ trống đơn vị vận chuyển → lỗi dưới field
- [x] Thành công → Shipping + lịch sử + buyer thấy shipper



### Thông báo

- [x] Đặt đơn thành công → seller có notification trong DB
- [x] Badge tăng
- [x] Click mở đúng đơn
- [x] Đánh dấu đã đọc / đọc tất cả
- [x] Tạo đơn thất bại → không có notification rác



### Dark mode

- [x] Áp dụng toàn site (header, modal, form, toast-like messages, empty state)
- [x] Refresh giữ preference
- [x] Theo hệ thống đổi theo OS
- [x] Không còn nền trắng chói / text mất contrast

---



## 10. Phần chưa hoàn thành / lưu ý


| Hạng mục                 | Trạng thái              | Ghi chú                                                                                                                  |
| ------------------------ | ----------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| Google Client ID         | Cần bạn điền vào `.env` | Thêm `Google__ClientId` và `VITE_GOOGLE_CLIENT_ID` (cùng Client ID), rồi `docker compose up -d --build frontend backend` |
| Migration trên Docker DB | **Đã apply**            | `AddGoogleAuthNotificationsAndAddressCodes` + `AddProductViewCount` đã chạy khi backend start                            |
| Realtime notification    | **Đã làm**              | SignalR event `NotificationReceived` qua group `user:{id}`; fallback poll 60–120s nếu mất socket                         |
| Lượt xem sản phẩm        | **Đã làm**              | `Products.ViewCount`, tăng khi người khác xem sản phẩm `Available`                                                       |


---



## 11. Hướng dẫn cấu hình nhanh

```bash
# 1. Env
cp .env.example .env
# Điền Google__ClientId và VITE_GOOGLE_CLIENT_ID (cùng Client ID)

# 2. Frontend env
# frontend/.env.local
VITE_API_BASE_URL=http://localhost:8081/api
VITE_GOOGLE_CLIENT_ID=....apps.googleusercontent.com

# 3. Migration
cd backend
dotnet ef database update --project src/PassDo.Infrastructure --startup-project src/PassDo.Api

# 4. Chạy
dotnet run --project src/PassDo.Api
cd ../frontend && npm run dev
```

Google Cloud Console:

1. Tạo OAuth Client ID kiểu **Web application**
2. Authorized JavaScript origins: `http://localhost:5173`, `http://localhost:3000`
3. Copy Client ID vào Backend + Frontend env

---

*Tài liệu đồng bộ với implementation tại thời điểm hoàn thành đợt nâng cấp này.*