import { useState } from 'react'
import { Button, PageHeader, Section } from '../components/common/ui'
import { PasswordInput } from '../components/common/PasswordInput'
import { PasswordStrengthMeter } from '../components/auth/PasswordStrengthMeter'
import { useAuthStore } from '../stores/authStore'
import { authApi } from '../features/auth/api'
import { isPasswordStrong } from '../lib/passwordStrength'
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
    if (!isPasswordStrong(newPassword)) {
      setError('Mật khẩu mới chưa đủ mạnh')
      return
    }

    setSaving(true)
    try {
      await authApi.changePassword({ currentPassword, newPassword })
      setSuccess('Đổi mật khẩu thành công. Vui lòng đăng nhập lại.')
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setTimeout(() => logout(), 2000)
    } catch (err) {
      setError(getErrorMessage(err, 'Không thể đổi mật khẩu.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Section className="max-w-lg">
      <PageHeader title="Tài khoản & Bảo mật" description="Quản lý bảo mật tài khoản của bạn." />

      <div className="space-y-6">
        <div className="rounded-2xl border border-line bg-surface/80 p-6">
          <h2 className="mb-4 font-display text-lg text-ink">Thông tin tài khoản</h2>
          <div className="space-y-2 text-sm">
            <p>
              <span className="text-muted">Email:</span> {user?.email}
            </p>
            <p>
              <span className="text-muted">Họ tên:</span> {user?.fullName}
            </p>
            <p>
              <span className="text-muted">Vai trò:</span> {user?.role}
            </p>
          </div>
        </div>

        <form
          onSubmit={handleChangePassword}
          className="space-y-4 rounded-2xl border border-line bg-surface/80 p-6"
        >
          <h2 className="font-display text-lg text-ink">Đổi mật khẩu</h2>
          <PasswordInput
            label="Mật khẩu hiện tại"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            required
          />
          <PasswordInput
            label="Mật khẩu mới"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
          />
          <PasswordStrengthMeter password={newPassword} />
          <PasswordInput
            label="Xác nhận mật khẩu mới"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            required
          />
          {error && <p className="text-sm text-rose-700">{error}</p>}
          {success && <p className="text-sm text-emerald-700">{success}</p>}
          <Button type="submit" disabled={saving} className="w-full">
            {saving ? 'Đang đổi...' : 'Đổi mật khẩu'}
          </Button>
        </form>
      </div>
    </Section>
  )
}
