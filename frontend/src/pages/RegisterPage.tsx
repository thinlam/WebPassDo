import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { authApi } from '../features/auth/api'
import { useAuthStore } from '../stores/authStore'
import { Button, Input, PageHeader, Section } from '../components/common/ui'
import { PasswordInput } from '../components/common/PasswordInput'
import { PasswordStrengthMeter } from '../components/auth/PasswordStrengthMeter'
import { GoogleSignInButton } from '../components/auth/GoogleSignInButton'
import { isPasswordStrong } from '../lib/passwordStrength'
import { getErrorMessage } from '../utils/api'

const schema = z
  .object({
    fullName: z.string().min(2, 'Nhập họ tên'),
    email: z.email('Email không hợp lệ'),
    phoneNumber: z.string().optional(),
    password: z
      .string()
      .min(8, 'Tối thiểu 8 ký tự')
      .refine(isPasswordStrong, 'Mật khẩu chưa đủ mạnh'),
    confirmPassword: z.string().min(1, 'Xác nhận mật khẩu'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Mật khẩu xác nhận không khớp',
    path: ['confirmPassword'],
  })

type FormValues = z.infer<typeof schema>

export function RegisterPage() {
  const navigate = useNavigate()
  const setSession = useAuthStore((s) => s.setSession)
  const [error, setError] = useState('')
  const [googleLoading, setGoogleLoading] = useState(false)
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const password = watch('password') ?? ''

  const onSubmit = handleSubmit(async (values) => {
    setError('')
    try {
      const session = await authApi.register({
        fullName: values.fullName,
        email: values.email,
        phoneNumber: values.phoneNumber || undefined,
        password: values.password,
      })
      setSession(session)
      navigate('/')
    } catch (err) {
      setError(getErrorMessage(err, 'Đăng ký thất bại'))
    }
  })

  const onGoogle = async (idToken: string) => {
    setError('')
    setGoogleLoading(true)
    try {
      const session = await authApi.googleLogin(idToken)
      setSession(session)
      if (!session.user.phoneNumber) {
        navigate('/settings?tab=addresses')
      } else {
        navigate('/')
      }
    } catch (err) {
      setError(getErrorMessage(err, 'Đăng ký Google thất bại'))
      throw err
    } finally {
      setGoogleLoading(false)
    }
  }

  return (
    <Section className="max-w-lg">
      <PageHeader title="Tạo tài khoản" description="Bắt đầu đăng bán và pass đồ cá nhân." />
      <form onSubmit={onSubmit} className="space-y-4 rounded-2xl border border-line bg-surface/80 p-6">
        <Input label="Họ và tên" error={errors.fullName?.message} {...register('fullName')} />
        <Input label="Email" type="email" error={errors.email?.message} {...register('email')} />
        <Input
          label="Số điện thoại"
          error={errors.phoneNumber?.message}
          {...register('phoneNumber')}
        />
        <PasswordInput
          label="Mật khẩu"
          error={errors.password?.message}
          autoComplete="new-password"
          {...register('password')}
        />
        <PasswordStrengthMeter password={password} />
        <PasswordInput
          label="Xác nhận mật khẩu"
          error={errors.confirmPassword?.message}
          autoComplete="new-password"
          {...register('confirmPassword')}
        />
        {error && <p className="text-sm text-rose-700">{error}</p>}
        <Button type="submit" disabled={isSubmitting || googleLoading} className="w-full">
          {isSubmitting ? 'Đang tạo...' : 'Đăng ký'}
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
          Đã có tài khoản?{' '}
          <Link to="/login" className="text-forest">
            Đăng nhập
          </Link>
        </p>
      </form>
    </Section>
  )
}
