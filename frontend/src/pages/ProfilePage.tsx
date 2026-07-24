import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { authApi } from '../features/auth/api'
import { useAuthStore } from '../stores/authStore'
import { Button, Input, PageHeader, Section, Spinner } from '../components/common/ui'
import { getErrorMessage } from '../utils/api'

const schema = z.object({
  fullName: z.string().min(2),
  phoneNumber: z.string().optional(),
  avatarUrl: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

export function ProfilePage() {
  const setSession = useAuthStore((s) => s.setSession)
  const accessToken = useAuthStore((s) => s.accessToken)
  const refreshToken = useAuthStore((s) => s.refreshToken)
  const queryClient = useQueryClient()
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: () => authApi.me(),
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (meQuery.data) {
      reset({
        fullName: meQuery.data.fullName,
        phoneNumber: meQuery.data.phoneNumber ?? '',
        avatarUrl: meQuery.data.avatarUrl ?? '',
      })
    }
  }, [meQuery.data, reset])

  const updateMutation = useMutation({
    mutationFn: (values: FormValues) => authApi.updateMe(values),
    onSuccess: (user) => {
      if (accessToken && refreshToken) {
        setSession({ accessToken, refreshToken, user })
      }
      queryClient.setQueryData(['me'], user)
      setMessage('Đã cập nhật hồ sơ')
      setError('')
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  if (meQuery.isLoading) return <Spinner />

  return (
    <Section className="max-w-lg">
      <PageHeader title="Hồ sơ" description={meQuery.data?.email} />
      <form
        className="space-y-4 rounded-2xl border border-line bg-white/80 p-6"
        onSubmit={handleSubmit((values) => updateMutation.mutate(values))}
      >
        <Input label="Họ tên" error={errors.fullName?.message} {...register('fullName')} />
        <Input label="Số điện thoại" error={errors.phoneNumber?.message} {...register('phoneNumber')} />
        <Input label="Avatar URL" error={errors.avatarUrl?.message} {...register('avatarUrl')} />
        <p className="text-sm text-muted">Vai trò: {meQuery.data?.role}</p>
        {message && <p className="text-sm text-emerald-800">{message}</p>}
        {error && <p className="text-sm text-rose-700">{error}</p>}
        <Button type="submit" disabled={isSubmitting || updateMutation.isPending}>
          Lưu thay đổi
        </Button>
      </form>
    </Section>
  )
}
