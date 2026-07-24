import { useState } from 'react'
import { Button, Input, PageHeader, Section } from '../components/common/ui'
import { useAuthStore } from '../stores/authStore'
import { apiClient } from '../api/client'
import { getErrorMessage } from '../utils/api'

export function AccountSecurityPage() {
  const user = useAuthStore((s) => s.user)
  const logout = useAuthStore((s) => s.logout)
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setSuccess('')

    if (newPassword !== confirmPassword) {
      setError('Mật khẩu xác nhận không khớp')
      return
    }
    if (newPassword.length < 6) {
      setError('Mật khẩu mới phải có ít nhất 6 ký tự')
      return
    }

    setSaving(true)
    try {
      await apiClient.post('/auth/change-password', {
        currentPassword,
        newPassword,
      })
      setSuccess('Đổi mật khẩu thành công. Vui lòng đăng nhập lại.')
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setTimeout(() => logout(), 2000)
    } catch (err) {
      setError(getErrorMessage(err, 'Không thể đổi mật khẩu. API chưa sẵn sàng hoặc mật khẩu hiện tại không đúng.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Section className="max-w-lg">
      <PageHeader title="Tài khoản & Bảo mật" description="Quản lý bảo mật tài khoản của bạn." />

      <div className="space-y-6">
        <div className="rounded-2xl border border-line bg-white/80 p-6">
          <h2 className="mb-4 font-display text-lg text-ink">Thông tin tài khoản</h2>
          <div className="space-y-2 text-sm">
            <p><span className="text-muted">Email:</span> {user?.email}</p>
            <p><span className="text-muted">Họ tên:</span> {user?.fullName}</p>
            <p><span className="text-muted">Vai trò:</span> {user?.role}</p>
          </div>
        </div>

        <form onSubmit={handleChangePassword} className="space-y-4 rounded-2xl border border-line bg-white/80 p-6">
          <h2 className="font-display text-lg text-ink">Đổi mật khẩu</h2>
          <Input
            label="Mật khẩu hiện tại"
            type="password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            required
          />
          <Input
            label="Mật khẩu mới"
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
          />
          <Input
            label="Xác nhận mật khẩu mới"
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            required
          />
          {error && <p className="text-sm text-rose-700">{error}</p>}
          {success && <p className="text-sm text-emerald-700">{success}</p>}
          <Button type="submit" disabled={saving}>
            {saving ? 'Đang lưu...' : 'Đổi mật khẩu'}
          </Button>
        </form>

        <div className="rounded-2xl border border-line bg-white/80 p-6">
          <h2 className="mb-3 font-display text-lg text-ink">Đăng xuất</h2>
          <p className="mb-4 text-sm text-muted">Đăng xuất khỏi tài khoản trên thiết bị này.</p>
          <Button variant="danger" onClick={logout}>
            Đăng xuất
          </Button>
        </div>
      </div>
    </Section>
  )
}
