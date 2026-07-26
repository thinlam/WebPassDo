import { GoogleLogin, type CredentialResponse } from '@react-oauth/google'
import { useState } from 'react'

type Props = {
  onSuccess: (idToken: string) => Promise<void> | void
  onError?: (message: string) => void
  disabled?: boolean
}

export function GoogleSignInButton({ onSuccess, onError, disabled }: Props) {
  const [loading, setLoading] = useState(false)
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined

  if (!clientId) {
    return (
      <p className="rounded-md border border-dashed border-line bg-sand/50 px-3 py-2 text-center text-xs text-muted">
        Chưa cấu hình VITE_GOOGLE_CLIENT_ID — đăng nhập Google tạm thời không khả dụng.
      </p>
    )
  }

  const handleSuccess = async (response: CredentialResponse) => {
    if (!response.credential) {
      onError?.('Không nhận được token từ Google.')
      return
    }
    setLoading(true)
    try {
      await onSuccess(response.credential)
    } catch (err) {
      onError?.(err instanceof Error ? err.message : 'Đăng nhập Google thất bại')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={`flex flex-col items-center gap-2 ${disabled || loading ? 'pointer-events-none opacity-60' : ''}`}>
      {loading && <p className="text-xs text-muted">Đang xác thực Google...</p>}
      <GoogleLogin
        onSuccess={handleSuccess}
        onError={() => onError?.('Bạn đã hủy hoặc đăng nhập Google thất bại.')}
        theme="outline"
        size="large"
        width="320"
        text="continue_with"
        shape="rectangular"
      />
    </div>
  )
}
