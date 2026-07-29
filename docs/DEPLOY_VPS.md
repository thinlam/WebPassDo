# Deploy PassDo lên VPS (Docker Compose)

Mục tiêu: **tắt máy cá nhân vẫn mở được web**. Local giữ `http://localhost:3000`; production dùng domain riêng (vd. `https://passdo.vn`).

## 0) Chuẩn bị mua / đăng ký

| Thứ | Gợi ý |
|-----|--------|
| VPS | 2 vCPU, **≥ 4 GB RAM** (SQL Server nặng), 40–80 GB SSD, Ubuntu 22.04/24.04 |
| Nhà cung cấp | Contabo, DigitalOcean, Linode, Azdigi, Viettel Cloud… |
| Domain | `.com` / `.vn` (Namecheap, Cloudflare, Nhà đăng ký VN) |
| DNS | A record `@` và `www` → **IP public** của VPS |

SSH vào VPS (thay IP):

```bash
ssh root@YOUR_VPS_IP
```

## 1) Cài Docker trên VPS

```bash
apt update && apt upgrade -y
curl -fsSL https://get.docker.com | sh
systemctl enable --now docker
docker compose version
```

Mở firewall (UFW):

```bash
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
# Không mở 1434 / 8081 ra internet
ufw enable
ufw status
```

## 2) Đưa code lên VPS

```bash
apt install -y git
mkdir -p /opt && cd /opt
git clone https://github.com/thinlam/WebPassDo.git
cd WebPassDo
git checkout Developer   # hoặc main sau khi merge
cp .env.example .env
nano .env
```

## 3) `.env` production (bắt buộc đổi)

```env
MSSQL_SA_PASSWORD=MatKhauSqlRatManh_CoChuHoaSoKyTu!
MSSQL_DATABASE=PassDoDb
MSSQL_PORT=1434
MSSQL_BIND=127.0.0.1

JWT_KEY=mot-chuoi-bi-mat-dai-hon-32-ky-tu-ngau-nhien
JWT_ISSUER=PassDo.Api
JWT_AUDIENCE=PassDo.Client

Google__ClientId=YOUR_CLIENT_ID.apps.googleusercontent.com
VITE_GOOGLE_CLIENT_ID=YOUR_CLIENT_ID.apps.googleusercontent.com

ASPNETCORE_ENVIRONMENT=Production
APPLY_MIGRATIONS=true
SWAGGER_ENABLED=false

BACKEND_PORT=8081
BACKEND_BIND=127.0.0.1
FRONTEND_PORT=80

VITE_API_BASE_URL=/api

# CORS = đúng URL người dùng mở trên trình duyệt
CORS_ORIGIN_0=https://passdo.vn
CORS_ORIGIN_1=https://www.passdo.vn
CORS_ORIGIN_2=http://localhost:3000
```

Đổi `passdo.vn` thành domain thật. Lần đầu có thể `APPLY_MIGRATIONS=true`; sau khi ổn định đổi `false` nếu muốn kiểm soát migration thủ công.

## 4) Chạy stack

```bash
cd /opt/WebPassDo
docker compose up -d --build
docker compose ps
curl -I http://127.0.0.1/
curl -s http://127.0.0.1/api/../health || curl -s http://127.0.0.1:8081/health
```

Mở `http://YOUR_VPS_IP` trên trình duyệt (chưa HTTPS). Nếu OK → làm bước domain + HTTPS.

## 5) HTTPS bằng Caddy (khuyến nghị)

Cài Caddy, để Caddy lắng nghe 80/443 và proxy vào frontend container.

Đổi frontend không chiếm port 80 của host:

```env
FRONTEND_PORT=3000
```

```bash
docker compose up -d frontend
```

`/etc/caddy/Caddyfile`:

```caddy
passdo.vn, www.passdo.vn {
        reverse_proxy 127.0.0.1:3000
}
```

```bash
systemctl reload caddy
```

Caddy tự xin Let’s Encrypt. Sau đó chỉ dùng `https://passdo.vn`.

## 6) Google OAuth

Trong [Google Cloud Console](https://console.cloud.google.com/) → Credentials → OAuth Client:

**Authorized JavaScript origins**

- `http://localhost:3000` (local)
- `https://passdo.vn`
- `https://www.passdo.vn`

**Authorized redirect URIs** (nếu Console yêu cầu)

- `http://localhost:3000`
- `https://passdo.vn`

Sau khi đổi Client ID / `VITE_*`:

```bash
docker compose up -d --build frontend backend
```

(`VITE_GOOGLE_CLIENT_ID` chỉ có hiệu lực khi **rebuild** frontend.)

## 7) Mang data từ máy local lên VPS (optional)

Trên máy Windows: backup theo [README — Đổi máy](../README.md).

Copy lên VPS:

```bash
scp -r PassDoBackup root@YOUR_VPS_IP:/opt/PassDoBackup
```

Trên VPS (chỉnh path ngày):

```bash
cd /opt/WebPassDo
# nạp .env…
export $(grep -v '^#' .env | xargs)

docker compose up -d database
sleep 30
docker exec passdo-database mkdir -p /var/opt/mssql/backup
docker cp /opt/PassDoBackup/YYYY-MM-DD/PassDoDb.bak passdo-database:/var/opt/mssql/backup/PassDoDb.bak

docker exec passdo-database /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
  -Q "RESTORE DATABASE [PassDoDb] FROM DISK = N'/var/opt/mssql/backup/PassDoDb.bak' WITH REPLACE"

docker compose up -d backend
sleep 15
docker run --rm \
  -v webpassdo_uploads_data:/data \
  -v /opt/PassDoBackup/YYYY-MM-DD:/backup \
  alpine sh -c "tar xzf /backup/uploads.tar.gz -C /data"

docker compose up -d
```

Trước restore nên tạm `APPLY_MIGRATIONS=false` nếu DB đã có schema từ bản backup.

## 8) Local vs Production

| | Local (PC) | Production (VPS) |
|--|------------|------------------|
| URL | `http://localhost:3000` | `https://passdo.vn` |
| Tắt PC | Mất | Vẫn chạy |
| Code | Git pull / Docker Desktop | Git pull + `compose up --build` |
| Data | Volume Docker trên PC | Volume trên VPS (backup định kỳ) |

Hai môi trường **không tự đồng bộ DB**. Đổi máy dev vẫn dùng bak như README; production backup riêng trên VPS (cron + `.bak`).

## 9) Cập nhật code sau này

```bash
cd /opt/WebPassDo
git pull
docker compose up -d --build
```

## 10) Checklist trước khi “go live”

- [ ] Password SQL + JWT mạnh (không copy từ `.env.example`)
- [ ] `SWAGGER_ENABLED=false`
- [ ] SQL / API chỉ bind `127.0.0.1`
- [ ] Firewall chỉ 22, 80, 443
- [ ] HTTPS OK
- [ ] Google origins có domain production
- [ ] `CORS_ORIGIN_*` đúng `https://...`
- [ ] Backup `.bak` + uploads định kỳ (USB / Object Storage)

---

**Bước tiếp theo của bạn:** thuê VPS + (optional) domain, gửi IP/domain — có thể hướng dẫn SSH từng lệnh trên server thật.
