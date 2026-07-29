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

**Tài khoản & quyền:** Hệ thống chỉ có role `Admin` và `User`. Một tài khoản `User` vừa đăng bán sản phẩm vừa mua hàng của người khác — không cần đổi role. Không còn role Shipper (giao hàng do người bán bàn giao / đơn vị vận chuyển).

Chi tiết luồng mua bán / thanh toán / vận chuyển: [docs/COMMERCE_HANDOVER.md](docs/COMMERCE_HANDOVER.md).

**Deploy lên VPS (tắt máy vẫn mở web):** [docs/DEPLOY_VPS.md](docs/DEPLOY_VPS.md).

### Ghi chú Windows

- Port host `8080` thường bị Windows chặn → mặc định map Backend ra **`8081`**.
- Port host `1433` thường bị SQL Express chiếm → Docker SQL map ra **`1434`**.
- Đổi port trong `.env` nếu cần: `BACKEND_PORT`, `FRONTEND_PORT`, `MSSQL_PORT`.

### EF Core Migrations (local + Docker)

Luôn chạy từ **root repo** (`E:\WebPassDo`), không chạy trong thư mục `Migrations` (sẽ lỗi `No project was found`).

```powershell
cd E:\WebPassDo

# Tạo migration mới (sau khi đổi entity / DbContext)
dotnet ef migrations add <TenMigration> `
  --project backend/src/PassDo.Infrastructure/PassDo.Infrastructure.csproj `
  --startup-project backend/src/PassDo.Api/PassDo.Api.csproj `
  --output-dir Persistence/Migrations

# Ví dụ PASSDO-02:
# dotnet ef migrations add RemoveLegacyShipperFields `
#   --project backend/src/PassDo.Infrastructure/PassDo.Infrastructure.csproj `
#   --startup-project backend/src/PassDo.Api/PassDo.Api.csproj `
#   --output-dir Persistence/Migrations
```

#### 1) Update database — SQL local (SQL Express)

Connection lấy từ `appsettings.Development.json` / `appsettings.json` (`DESKTOP-...\SQLEXPRESS`, database `PassDoDb`):

```powershell
cd E:\WebPassDo
dotnet ef database update `
  --project backend/src/PassDo.Infrastructure/PassDo.Infrastructure.csproj `
  --startup-project backend/src/PassDo.Api/PassDo.Api.csproj
```

#### 2) Update database — Docker SQL (`localhost:1434`)

**Cách A (khuyến nghị):** Backend tự migrate khi start nếu `APPLY_MIGRATIONS=true` trong `.env` (mặc định Development):

```powershell
cd E:\WebPassDo
docker compose up -d --build backend
# hoặc cả stack:
# docker compose up -d --build
```

**Cách B:** Chạy `ef database update` thẳng vào SQL Docker:

```powershell
cd E:\WebPassDo

# Nạp MSSQL_SA_PASSWORD từ .env
Get-Content .env | ForEach-Object {
  if ($_ -match '^\s*#' -or $_ -notmatch '=') { return }
  $k, $v = $_.Split('=', 2)
  Set-Item -Path "Env:$k" -Value $v.Trim()
}

$env:ConnectionStrings__DefaultConnection = "Server=localhost,$($env:MSSQL_PORT);Database=$($env:MSSQL_DATABASE);User Id=sa;Password=$($env:MSSQL_SA_PASSWORD);TrustServerCertificate=True;Encrypt=False"

dotnet ef database update `
  --project backend/src/PassDo.Infrastructure/PassDo.Infrastructure.csproj `
  --startup-project backend/src/PassDo.Api/PassDo.Api.csproj
```

| Môi trường | DB | Cách apply migration |
| ---------- | -- | -------------------- |
| `dotnet run` local | SQL Express | `dotnet ef database update` (mục 1) |
| Docker Compose | SQL container `:1434` | `APPLY_MIGRATIONS=true` + restart backend, hoặc mục 2B |

**Không cần** `docker compose down -v` chỉ vì có migration mới — volume giữ nguyên, schema được update. Chỉ reset volume khi cố ý xóa sạch DB/uploads:

```bash
docker compose down
docker compose down -v   # xóa DB + uploads — mất dữ liệu
```

Production: đặt `APPLY_MIGRATIONS=false` và chạy migration có kiểm soát.

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

Image SQL 2022 hiện tại chỉ có **`/opt/mssql-tools18/bin/sqlcmd`** (kèm flag `-C`). Đường `/opt/mssql-tools/bin/sqlcmd` thường **không tồn tại** — nếu thấy lỗi `no such file or directory` thì đang gọi nhầm path cũ.

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

### 3) Đồng bộ 2 chiều (máy mới ↔ máy cũ) — quy trình + lệnh

**Git chỉ đồng bộ code.** Data (user, đơn, chat, ảnh) phải **backup máy vừa làm xong → mang USB/Drive → restore máy kia**.

Giả sử trên mỗi máy:

| | Ví dụ path |
|--|------------|
| Code | `D:\WebPassDo` (hoặc `E:\WebPassDo`) |
| Thư mục mang đi | `D:\PassDoBackup` (USB / ổ di động cũng được) |

Đổi `D:\` cho đúng ổ máy bạn.

---

#### Bước A — Rời máy (backup data + đẩy code)

Chạy trên **máy đang có data mới nhất** trước khi tắt / đổi máy:

```powershell
cd D:\WebPassDo

# 1) Code lên remote (nếu đã commit)
git status
git push -u origin HEAD

# 2) Nạp .env
Get-Content .env | ForEach-Object {
  if ($_ -match '^\s*#' -or $_ -notmatch '=') { return }
  $k, $v = $_.Split('=', 2)
  Set-Item -Path "Env:$k" -Value $v.Trim()
}

# 3) Thư mục backup (đặt tên theo ngày cho dễ)
$stamp = Get-Date -Format "yyyy-MM-dd"
$bakDir = "D:\PassDoBackup\$stamp"
New-Item -ItemType Directory -Force -Path $bakDir | Out-Null

docker compose up -d database
Start-Sleep 25
docker exec passdo-database mkdir -p /var/opt/mssql/backup

docker exec passdo-database /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "$env:MSSQL_SA_PASSWORD" -C `
  -Q "BACKUP DATABASE [PassDoDb] TO DISK = N'/var/opt/mssql/backup/PassDoDb.bak' WITH INIT"

docker cp passdo-database:/var/opt/mssql/backup/PassDoDb.bak "$bakDir\PassDoDb.bak"

docker run --rm `
  -v webpassdo_uploads_data:/data `
  -v ${bakDir}:/backup `
  alpine tar czf /backup/uploads.tar.gz -C /data .

Copy-Item .env "$bakDir\.env" -Force
Get-ChildItem $bakDir
```

**Check:** `PassDoDb.bak` vài MB trở lên. Mang cả folder `$bakDir` (hoặc cả `D:\PassDoBackup`) sang máy kia.

---

#### Bước B — Tới máy kia (kéo code + restore data)

Chạy trên **máy nhận**. Ví dụ ổ **E:** (đổi drive nếu khác):

```powershell
cd E:\WebPassDo

# 1) Code mới nhất
git pull

# 2) Trỏ tới folder backup vừa mang sang (đổi ngày cho đúng)
$bakDir = "E:\PassDoBackup\2026-07-25"   # <-- sửa ngày/path

Copy-Item "$bakDir\.env" .\.env -Force

Get-Content .env | ForEach-Object {
  if ($_ -match '^\s*#' -or $_ -notmatch '=') { return }
  $k, $v = $_.Split('=', 2)
  Set-Item -Path "Env:$k" -Value $v.Trim()
}

# Tránh seed đè data restore
(Get-Content .env) -replace 'APPLY_MIGRATIONS=true', 'APPLY_MIGRATIONS=false' | Set-Content .env

# Nếu DB máy này lệch / seed cũ → xóa volume rồi restore sạch (CẨN THẬN: mất data local chưa backup)
# docker compose down -v

docker compose up -d database
Start-Sleep 30

docker exec passdo-database mkdir -p /var/opt/mssql/backup
docker cp "$bakDir\PassDoDb.bak" passdo-database:/var/opt/mssql/backup/PassDoDb.bak

docker exec passdo-database /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "$env:MSSQL_SA_PASSWORD" -C `
  -Q "RESTORE DATABASE [PassDoDb] FROM DISK = N'/var/opt/mssql/backup/PassDoDb.bak' WITH REPLACE"

docker compose up -d backend
Start-Sleep 15

docker run --rm `
  -v webpassdo_uploads_data:/data `
  -v ${bakDir}:/backup `
  alpine sh -c "tar xzf /backup/uploads.tar.gz -C /data"

docker compose up --build -d
```

Mở http://localhost:3000 → login **user đã có trên máy vừa backup**. Thấy sản phẩm/đơn giống nhau = OK.


---

#### Ví dụ lịch làm việc

```text
Thứ 2–4: làm trên Máy mới (D:)
  → hết ngày / đổi máy: chạy Bước A (backup + git push)

Thứ 5: về Máy cũ (E:)
  → chạy Bước B (git pull + restore folder backup mới nhất)
  → làm tiếp…
  → trước khi tắt: lại Bước A

Thứ 6: lại Máy mới
  → Bước B với bản backup vừa lấy từ Máy cũ
```

Luôn coi **máy vừa backup gần nhất** là nguồn sự thật của data.

---

#### Không làm

- Chỉ `git pull` rồi mong thấy đơn/sản phẩm mới → **không có**.
- Restore `.bak` cũ hơn lên máy đã có data mới → **mất** data mới (`WITH REPLACE`).
- Commit `.bak` / `.env` / uploads vào git → để ngoài repo (USB / Drive).

**Hai máy dùng song song lâu dài:** nên 1 SQL/cloud chung; Docker local + bak chỉ hợp khi **một máy làm chính rồi chuyển**.

## Chạy local (không Docker)

### Backend

Cập nhật `ConnectionStrings:DefaultConnection` trong `appsettings.Development.json` cho SQL Express / SQL Server local, rồi apply migration + chạy API (xem thêm mục **EF Core Migrations** ở trên):

```powershell
cd E:\WebPassDo
dotnet ef database update `
  --project backend/src/PassDo.Infrastructure/PassDo.Infrastructure.csproj `
  --startup-project backend/src/PassDo.Api/PassDo.Api.csproj
dotnet run --project backend/src/PassDo.Api/PassDo.Api.csproj
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
