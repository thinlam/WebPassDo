import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { authApi } from '../features/auth/api'
import { useAuthStore } from '../stores/authStore'
import { Button, Input, PageHeader, Section } from '../components/common/ui'
import { getErrorMessage } from '../utils/api'

const schema = z.object({
  fullName: z.string().min(2, 'Nhập họ tên'),
  email: z.email('Email không hợp lệ'),
  phoneNumber: z.string().optional(),
  password: z.string().min(8, 'Tối thiểu 8 ký tự'),
})

type FormValues = z.infer<typeof schema>

export function RegisterPage() {
  const navigate = useNavigate()
  const setSession = useAuthStore((s) => s.setSession)
  const [error, setError] = useState('')
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const onSubmit = handleSubmit(async (values) => {
    setError('')
    try {
      const session = await authApi.register(values)
      setSession(session)
      navigate('/')
    } catch (err) {
      setError(getErrorMessage(err, 'Đăng ký thất bại'))
    }
  })

  return (
    <Section className="max-w-lg">
      <PageHeader title="Tạo tài khoản" description="Bắt đầu đăng bán và pass đồ cá nhân." />
      <form onSubmit={onSubmit} className="space-y-4 rounded-2xl border border-line bg-white/80 p-6">
        <Input label="Họ tên" error={errors.fullName?.message} {...register('fullName')} />
        <Input label="Email" type="email" error={errors.email?.message} {...register('email')} />
        <Input label="Số điện thoại" error={errors.phoneNumber?.message} {...register('phoneNumber')} />
        <Input
          label="Mật khẩu"
          type="password"
          error={errors.password?.message}
          {...register('password')}
        />
        {error && <p className="text-sm text-rose-700">{error}</p>}
        <Button type="submit" disabled={isSubmitting} className="w-full">
          {isSubmitting ? 'Đang tạo...' : 'Đăng ký'}
        </Button>
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
