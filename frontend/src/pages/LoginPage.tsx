import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { authApi } from '../features/auth/api'
import { useAuthStore } from '../stores/authStore'
import { Button, Input, PageHeader, Section } from '../components/common/ui'
import { PasswordInput } from '../components/common/PasswordInput'
import { GoogleSignInButton } from '../components/auth/GoogleSignInButton'
import { getErrorMessage } from '../utils/api'

const schema = z.object({
  email: z.string().min(1, 'Nhập email hoặc số điện thoại'),
  password: z.string().min(1, 'Nhập mật khẩu'),
})

type FormValues = z.infer<typeof schema>

export function LoginPage() {
  const navigate = useNavigate()
  const setSession = useAuthStore((s) => s.setSession)
  const [error, setError] = useState('')
  const [googleLoading, setGoogleLoading] = useState(false)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const onSubmit = handleSubmit(async (values) => {
    setError('')
    try {
      const session = await authApi.login(values)
      setSession(session)
      navigate('/')
    } catch (err) {
      setError(getErrorMessage(err, 'Đăng nhập thất bại'))
    }
  })

  const onGoogle = async (idToken: string) => {
    setError('')
    setGoogleLoading(true)
    try {
      const session = await authApi.googleLogin(idToken)
      setSession(session)
      navigate('/')
    } catch (err) {
      setError(getErrorMessage(err, 'Đăng nhập Google thất bại'))
      throw err
    } finally {
      setGoogleLoading(false)
    }
  }

  return (
    <Section className="max-w-lg">
      <PageHeader title="Đăng nhập" description="Tiếp tục pass đồ và mua bán lại." />
      <form onSubmit={onSubmit} className="space-y-4 rounded-2xl border border-line bg-surface/80 p-6">
        <Input
          label="Email hoặc số điện thoại"
          error={errors.email?.message}
          autoComplete="username"
          {...register('email')}
        />
        <PasswordInput
          label="Mật khẩu"
          error={errors.password?.message}
          autoComplete="current-password"
          {...register('password')}
        />
        {error && <p className="text-sm text-rose-700">{error}</p>}
        <Button type="submit" disabled={isSubmitting || googleLoading} className="w-full">
          {isSubmitting ? 'Đang đăng nhập...' : 'Đăng nhập'}
        </Button>

        <div className="flex items-center gap-3 py-1">
          <div className="h-px flex-1 bg-line" />
          <span className="text-xs text-muted">Hoặc</span>
          <div className="h-px flex-1 bg-line" />
        </div>

        <GoogleSignInButton
          onSuccess={onGoogle}
          onError={setError}
          disabled={isSubmitting || googleLoading}
        />

        <p className="text-center text-sm text-muted">
          Chưa có tài khoản?{' '}
          <Link to="/register" className="text-forest">
            Đăng ký
          </Link>
        </p>
      </form>
    </Section>
  )
}
