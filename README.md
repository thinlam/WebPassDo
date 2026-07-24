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

Shipper seed:

- Email: `shipper@passdo.local`
- Password: `Shipper@123456`

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
