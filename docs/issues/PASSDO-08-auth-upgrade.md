# PASSDO-08 — Nâng cấp đăng nhập và đăng ký

## Trạng thái

- [ ] Đã làm
- [ ] Đã cập nhật thêm

## Hiện trạng

| Hạng mục | Nội dung |
| -------- | -------- |
| Backend | ✅ Register/Login/Google/Refresh/Logout/ChangePassword. Hash password chuẩn. ❌ Forgot/reset, email verify flow, login lockout, rate limit |
| Frontend | ✅ Show/hide password, strength meter, Google, loading/disable submit cơ bản. ❌ Forgot password, Remember me, email verify UI |
| Database | ✅ Users, RefreshTokens. ❌ Token reset password / email verification columns nếu cần |
| API | `/api/auth/*` như hiện có |
| Lỗi hiện tại | 401 không thử refresh; build cần `@react-oauth/google` đã install |
| Việc tiếp theo | Forgot password + lockout + refresh-on-401 + Remember me |

## Checklist

### Đăng nhập

- [x] Hiện/ẩn mật khẩu
- [x] Google login
- [ ] Quên mật khẩu
- [ ] Ghi nhớ đăng nhập
- [x] Loading khi submit
- [x] Chống double submit (cơ bản)

### Đăng ký

- [x] Confirm password (cần xác nhận trên RegisterPage — có Zod)
- [x] Hiện/ẩn mật khẩu
- [x] Password strength
- [x] Rule hoa/thường/số/ký tự
- [ ] Xác minh email
- [x] Google register/login

### Bảo mật

- [x] Hash mật khẩu (không plaintext)
- [x] Refresh token (backend + store)
- [ ] Thu hồi token khi đổi mật khẩu (verify đầy đủ)
- [ ] Giới hạn đăng nhập sai
- [ ] FE dùng refresh trước khi logout khi 401

## Cần làm (chưa hoàn thành)

1. Forgot/Reset password (email link hoặc OTP)
2. Remember me / kéo dài refresh
3. Login rate limiting + lockout
4. Email verification
5. Axios interceptor: refresh rồi retry

## Definition of Done

Chưa đạt đủ checklist roadmap — đánh dấu **chưa hoàn thành**.
