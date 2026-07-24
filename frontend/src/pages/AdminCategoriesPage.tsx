import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { categoriesApi } from '../features/categories/api'
import { Badge, Button, EmptyState, Input, PageHeader, Section, Spinner, TextArea } from '../components/common/ui'
import { getErrorMessage } from '../utils/api'

const schema = z.object({
  name: z.string().min(2),
  description: z.string().optional(),
  displayOrder: z.number().min(0),
})

type FormValues = z.infer<typeof schema>

export function AdminCategoriesPage() {
  const queryClient = useQueryClient()
  const [error, setError] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)

  const query = useQuery({
    queryKey: ['categories', 'admin'],
    queryFn: () => categoriesApi.list(true),
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { displayOrder: 0 },
  })

  const createMutation = useMutation({
    mutationFn: (values: FormValues) =>
      categoriesApi.create({
        name: values.name,
        description: values.description,
        displayOrder: values.displayOrder,
        isActive: true,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] })
      reset({ name: '', description: '', displayOrder: 0 })
      setError('')
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, values }: { id: string; values: FormValues & { isActive: boolean } }) =>
      categoriesApi.update(id, {
        name: values.name,
        description: values.description,
        displayOrder: values.displayOrder,
        isActive: values.isActive,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] })
      setEditingId(null)
      reset({ name: '', description: '', displayOrder: 0 })
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => categoriesApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['categories'] }),
    onError: (err) => setError(getErrorMessage(err)),
  })

  return (
    <Section>
      <PageHeader title="Quản trị danh mục" description="Chỉ Admin được thêm/sửa/xóa danh mục." />
      {error && <p className="mb-4 text-sm text-rose-700">{error}</p>}

      <form
        className="mb-8 grid gap-3 rounded-2xl border border-line bg-white/80 p-4 md:grid-cols-4"
        onSubmit={handleSubmit((values) => {
          if (editingId) {
            const current = query.data?.find((c) => c.id === editingId)
            updateMutation.mutate({
              id: editingId,
              values: { ...values, isActive: current?.isActive ?? true },
            })
          } else {
            createMutation.mutate(values)
          }
        })}
      >
        <Input label="Tên" error={errors.name?.message} {...register('name')} />
        <TextArea label="Mô tả" rows={1} error={errors.description?.message} {...register('description')} />
        <Input
          label="Thứ tự"
          type="number"
          error={errors.displayOrder?.message}
          {...register('displayOrder', { valueAsNumber: true })}
        />
        <div className="flex items-end gap-2">
          <Button type="submit" disabled={isSubmitting}>
            {editingId ? 'Cập nhật' : 'Thêm'}
          </Button>
          {editingId && (
            <Button
              type="button"
              variant="ghost"
              onClick={() => {
                setEditingId(null)
                reset({ name: '', description: '', displayOrder: 0 })
              }}
            >
              Hủy
            </Button>
          )}
        </div>
      </form>

      {query.isLoading && <Spinner />}
      {query.data?.length === 0 && <EmptyState title="Chưa có danh mục" />}
      <div className="space-y-3">
        {query.data?.map((category) => (
          <div
            key={category.id}
            className="flex flex-col gap-3 rounded-2xl border border-line bg-white/80 p-4 sm:flex-row sm:items-center sm:justify-between"
          >
            <div>
              <p className="font-medium">{category.name}</p>
              <p className="text-sm text-muted">{category.description}</p>
              <div className="mt-1 flex gap-2">
                <Badge>{category.slug}</Badge>
                <Badge tone={category.isActive ? 'success' : 'danger'}>
                  {category.isActive ? 'Active' : 'Inactive'}
                </Badge>
              </div>
            </div>
            <div className="flex gap-2">
              <Button
                variant="secondary"
                onClick={() => {
                  setEditingId(category.id)
                  reset({
                    name: category.name,
                    description: category.description ?? '',
                    displayOrder: category.displayOrder,
                  })
                }}
              >
                Sửa
              </Button>
              <Button variant="danger" onClick={() => deleteMutation.mutate(category.id)}>
                Xóa
              </Button>
            </div>
          </div>
        ))}
      </div>
    </Section>
  )
}
