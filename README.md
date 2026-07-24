# PassDo App

Web app đăng bán lại / pass đồ cá nhân. Monorepo tách Backend (.NET 8 Clean Architecture) và Frontend (React + Vite + TypeScript).

## Cấu trúc

```text
WebPassDo/
├── backend/          # ASP.NET Core Web API
├── frontend/         # React + Vite + Nginx
├── docker-compose.yml
├── .env.example
└── README.md
```

## Yêu cầu

- .NET 8 SDK
- Node.js 22+
- Docker Desktop

## Chạy bằng Docker (khuyến nghị)

```bash
cd WebPassDo
cp .env.example .env
docker compose up --build
```

Sau khi healthy:

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| API qua Nginx | http://localhost:3000/api |
| Swagger | http://localhost:8081/swagger |
| Health | http://localhost:8081/health |
| Health qua Nginx | http://localhost:3000/health |
| SQL Server (Docker) | `localhost:1434` (sa / giá trị `MSSQL_SA_PASSWORD`) |

Admin seed:

- Email: `admin@passdo.local`
- Password: `Admin@123456`

(Tài khoản Shipper đã bỏ — giao hàng do seller bàn giao.)

Chi tiết luồng mua bán / thanh toán / vận chuyển: [docs/COMMERCE_HANDOVER.md](docs/COMMERCE_HANDOVER.md).

### Ghi chú Windows

- Port host `8080` thường bị Windows chặn → mặc định map Backend ra **`8081`**.
- Port host `1433` thường bị SQL Express chiếm → Docker SQL map ra **`1434`**.
- Đổi port trong `.env` nếu cần: `BACKEND_PORT`, `FRONTEND_PORT`, `MSSQL_PORT`.

### Migration trong Docker

- `APPLY_MIGRATIONS=true` (mặc định Development) → Backend tự `Migrate` + seed khi start.
- Production: đặt `APPLY_MIGRATIONS=false` và chạy migration có kiểm soát.

Dừng / reset sạch volume:

```bash
docker compose down
docker compose down -v   # xóa DB + uploads
```

## Đổi máy: backup DB + uploads (Docker)

Chỉ cần mang: **source code (git)** + **`.env`** + **`PassDoDb.bak`** + **`uploads.tar.gz`**. Không cần copy Docker image.

Volume mặc định (tên project `webpassdo`):

| Volume | Nội dung |
|--------|----------|
| `webpassdo_mssql_data` | SQL Server data |
| `webpassdo_uploads_data` | Ảnh sản phẩm (`/app/uploads`) |

### 1) Backup trên máy cũ (PowerShell)

```powershell
cd E:\WebPassDo   # hoặc path project của bạn

# Nạp biến từ .env vào session hiện tại
Get-Content .env | ForEach-Object {
  if ($_ -match '^\s*#' -or $_ -notmatch '=') { return }
  $k, $v = $_.Split('=', 2)
  Set-Item -Path "Env:$k" -Value $v.Trim()
}

New-Item -ItemType Directory -Force -Path E:\PassDoBackup | Out-Null
docker compose up -d database
Start-Sleep 25   # đợi DB healthy

docker exec passdo-database mkdir -p /var/opt/mssql/backup

docker exec passdo-database /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "$env:MSSQL_SA_PASSWORD" -C `
  -Q "BACKUP DATABASE [PassDoDb] TO DISK = N'/var/opt/mssql/backup/PassDoDb.bak' WITH INIT"

docker cp passdo-database:/var/opt/mssql/backup/PassDoDb.bak E:\PassDoBackup\PassDoDb.bak

docker run --rm `
  -v webpassdo_uploads_data:/data `
  -v E:\PassDoBackup:/backup `
  alpine tar czf /backup/uploads.tar.gz -C /data .

Copy-Item .env E:\PassDoBackup\.env -Force
Get-ChildItem E:\PassDoBackup
```

**Kiểm tra trước khi mang đi:**

- `PassDoDb.bak` phải khoảng **vài MB trở lên** (ví dụ ~8 MB). Không có file / lỗi `Could not find ...bak` = backup thất bại.
- `uploads.tar.gz` ~**85 bytes** = volume ảnh trống (OK nếu chưa upload ảnh). Có ảnh thì file sẽ lớn hơn rõ.
- Container DB phải **Running** trước khi backup (`docker compose up -d database`).
- Password lấy từ `.env` (`MSSQL_SA_PASSWORD`); đừng dựa vào `$env:...` nếu chưa nạp `.env`.

Nếu `sqlcmd` 18 không có, thử:

```powershell
docker exec passdo-database /opt/mssql-tools/bin/sqlcmd `
  -S localhost -U sa -P "$env:MSSQL_SA_PASSWORD" `
  -Q "BACKUP DATABASE [PassDoDb] TO DISK = N'/var/opt/mssql/backup/PassDoDb.bak' WITH INIT"
```

Tên volume uploads nếu khác: `docker volume ls | findstr uploads`.

### 2) Restore trên máy mới

```powershell
# Code: git clone / copy WebPassDo, rồi:
cd <path>\WebPassDo
Copy-Item <path>\PassDoBackup\.env .\.env

Get-Content .env | ForEach-Object {
  if ($_ -match '^\s*#' -or $_ -notmatch '=') { return }
  $k, $v = $_.Split('=', 2)
  Set-Item -Path "Env:$k" -Value $v.Trim()
}

# Tránh seed đè data vừa restore (lần đầu)
(Get-Content .env) -replace 'APPLY_MIGRATIONS=true', 'APPLY_MIGRATIONS=false' | Set-Content .env

# Nếu máy mới đã từng compose up (DB seed sẵn) → xóa volume cũ trước
# docker compose down -v

docker compose up -d database
Start-Sleep 30

docker exec passdo-database mkdir -p /var/opt/mssql/backup
docker cp <path>\PassDoBackup\PassDoDb.bak passdo-database:/var/opt/mssql/backup/PassDoDb.bak

docker exec passdo-database /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "$env:MSSQL_SA_PASSWORD" -C `
  -Q "RESTORE DATABASE [PassDoDb] FROM DISK = N'/var/opt/mssql/backup/PassDoDb.bak' WITH REPLACE"

# Backend tạo volume uploads, rồi giải nén ảnh
docker compose up -d backend
Start-Sleep 15

docker run --rm `
  -v webpassdo_uploads_data:/data `
  -v <path>\PassDoBackup:/backup `
  alpine sh -c "tar xzf /backup/uploads.tar.gz -C /data"

docker compose up --build -d
```

**Sau restore:** mở http://localhost:3000 và đăng nhập bằng **tài khoản cũ** (user/order/sản phẩm phải giống máy cũ). Chỉ thấy `admin@passdo.local` seed → chưa restore đúng hoặc volume cũ chưa xóa.

Khi ổn định có thể bật lại `APPLY_MIGRATIONS=true` nếu cần migrate schema mới (migration additive). Restore DB đã đủ schema thì để `false` cũng được.

## Chạy local (không Docker)

### Backend

Cập nhật `ConnectionStrings:DefaultConnection` trong `appsettings.Development.json` cho SQL Express / SQL Server local, rồi:

```bash
cd backend
dotnet ef database update --project src/PassDo.Infrastructure --startup-project src/PassDo.Api
dotnet run --project src/PassDo.Api
```

Local mặc định: http://localhost:8080/swagger

### Frontend

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

Frontend: http://localhost:5173  
`VITE_API_BASE_URL=/api` (Vite proxy sang Backend `8080`).

## Phase hoàn thành

**Phase 1–7** đã xong:

1. Project setup + Docker skeleton  
2. Backend foundation  
3. Authentication  
4. Category + Product + images + search  
5. Favorite + Order  
6. Frontend đầy đủ  
7. Hoàn thiện Docker  

## API tóm tắt

Swagger: http://localhost:8081/swagger

| Nhóm | Prefix |
|------|--------|
| Auth | `/api/auth`, `/api/users/me` |
| Categories | `/api/categories` |
| Products | `/api/products` |
| Favorites | `/api/favorites` |
| Orders | `/api/orders` |
