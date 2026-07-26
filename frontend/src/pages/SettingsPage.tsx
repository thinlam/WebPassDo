import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { addressesApi } from '../features/addresses/api'
import { bankAccountsApi } from '../features/bankAccounts/api'
import {
  Badge,
  Button,
  EmptyState,
  Input,
  PageHeader,
  Section,
  Select,
  Spinner,
} from '../components/common/ui'
import {
  VietnamAddressFields,
  type VietnamAddressValue,
} from '../components/address/VietnamAddressFields'
import { getErrorMessage } from '../utils/api'
import type { AddressType, UserAddress, UserBankAccount } from '../types'

type Tab = 'addresses' | 'banks'

export function SettingsPage() {
  const [searchParams] = useSearchParams()
  const tabParam = searchParams.get('tab')
  const [tab, setTab] = useState<Tab>(() => {
    if (tabParam === 'banks') return 'banks'
    return 'addresses'
  })

  useEffect(() => {
    if (tabParam === 'banks') setTab('banks')
    else if (tabParam === 'addresses') setTab('addresses')
  }, [tabParam])

  return (
    <Section>
      <PageHeader title="Cài đặt" description="Quản lý địa chỉ và tài khoản ngân hàng." />
      <div className="mb-6 flex gap-2 border-b border-line">
        <button
          className={`px-4 py-2 text-sm font-medium transition ${tab === 'addresses' ? 'border-b-2 border-forest text-forest' : 'text-muted hover:text-ink'}`}
          onClick={() => setTab('addresses')}
        >
          Địa chỉ
        </button>
        <button
          className={`px-4 py-2 text-sm font-medium transition ${tab === 'banks' ? 'border-b-2 border-forest text-forest' : 'text-muted hover:text-ink'}`}
          onClick={() => setTab('banks')}
        >
          Tài khoản ngân hàng
        </button>
      </div>
      {tab === 'addresses' ? <AddressesTab /> : <BankAccountsTab />}
    </Section>
  )
}

function AddressesTab() {
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState<UserAddress | null>(null)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState('')

  const query = useQuery({
    queryKey: ['my-addresses'],
    queryFn: () => addressesApi.list(),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => addressesApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-addresses'] }),
    onError: (err) => setError(getErrorMessage(err)),
  })

  const defaultMutation = useMutation({
    mutationFn: (id: string) => addressesApi.setDefault(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-addresses'] }),
    onError: (err) => setError(getErrorMessage(err)),
  })

  if (query.isLoading) return <Spinner />

  if (creating || editing) {
    return (
      <AddressForm
        address={editing}
        onDone={() => {
          setCreating(false)
          setEditing(null)
          queryClient.invalidateQueries({ queryKey: ['my-addresses'] })
        }}
        onCancel={() => {
          setCreating(false)
          setEditing(null)
        }}
      />
    )
  }

  return (
    <div className="space-y-3">
      {error && <p className="text-sm text-rose-700">{error}</p>}
      <Button onClick={() => setCreating(true)}>Thêm địa chỉ</Button>
      {query.data?.length === 0 && <EmptyState title="Chưa có địa chỉ nào" />}
      {query.data?.map((addr) => (
        <div
          key={addr.id}
          className="flex flex-col gap-3 rounded-2xl border border-line bg-white/80 p-4 sm:flex-row sm:items-center"
        >
          <div className="flex-1 space-y-1">
            <p className="font-medium text-ink">
              {addr.recipientName} · {addr.phoneNumber}
            </p>
            <p className="text-sm text-muted">{addr.fullAddress}</p>
            <div className="flex gap-2">
              <Badge>{addr.addressType === 'Home' ? 'Nhà' : 'Văn phòng'}</Badge>
              {addr.isDefault && <Badge tone="success">Mặc định</Badge>}
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            {!addr.isDefault && (
              <Button variant="secondary" onClick={() => defaultMutation.mutate(addr.id)}>
                Đặt mặc định
              </Button>
            )}
            <Button variant="secondary" onClick={() => setEditing(addr)}>
              Sửa
            </Button>
            <Button variant="danger" onClick={() => deleteMutation.mutate(addr.id)}>
              Xóa
            </Button>
          </div>
        </div>
      ))}
    </div>
  )
}

function AddressForm({
  address,
  onDone,
  onCancel,
}: {
  address: UserAddress | null
  onDone: () => void
  onCancel: () => void
}) {
  const [recipientName, setRecipientName] = useState(address?.recipientName ?? '')
  const [phoneNumber, setPhoneNumber] = useState(address?.phoneNumber ?? '')
  const [note, setNote] = useState(address?.note ?? '')
  const [addressType, setAddressType] = useState<AddressType>(
    (address?.addressType as AddressType) ?? 'Home',
  )
  const [isDefault, setIsDefault] = useState(address?.isDefault ?? false)
  const [vnAddress, setVnAddress] = useState<VietnamAddressValue>({
    provinceCode: address?.provinceCode ?? '',
    provinceName: address?.province ?? '',
    districtCode: address?.districtCode ?? '',
    districtName: address?.district ?? '',
    wardCode: address?.wardCode ?? '',
    wardName: address?.ward ?? '',
    addressLine: address?.streetAddress ?? '',
  })
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const errors: Record<string, string> = {}
    if (!recipientName.trim()) errors.recipientName = 'Nhập tên người nhận'
    if (!phoneNumber.trim()) errors.phoneNumber = 'Nhập số điện thoại'
    if (!vnAddress.provinceName) errors.provinceName = 'Chọn tỉnh/thành phố'
    if (!vnAddress.districtName) errors.districtName = 'Chọn quận/huyện'
    if (!vnAddress.wardName) errors.wardName = 'Chọn phường/xã'
    if (!vnAddress.addressLine.trim()) errors.addressLine = 'Nhập địa chỉ chi tiết'
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    setSaving(true)
    setError('')
    try {
      const payload = {
        recipientName: recipientName.trim(),
        phoneNumber: phoneNumber.trim(),
        province: vnAddress.provinceName,
        district: vnAddress.districtName,
        ward: vnAddress.wardName,
        provinceCode: vnAddress.provinceCode || null,
        districtCode: vnAddress.districtCode || null,
        wardCode: vnAddress.wardCode || null,
        streetAddress: vnAddress.addressLine.trim(),
        note: note.trim() || undefined,
        addressType,
        isDefault,
      }
      if (address) {
        await addressesApi.update(address.id, payload)
      } else {
        await addressesApi.create(payload)
      }
      onDone()
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <form
      className="max-w-lg space-y-4 rounded-2xl border border-line bg-surface/80 p-6"
      onSubmit={handleSubmit}
    >
      <h2 className="font-display text-xl text-ink">
        {address ? 'Sửa địa chỉ' : 'Thêm địa chỉ'}
      </h2>
      <Input
        label="Tên người nhận"
        value={recipientName}
        onChange={(e) => setRecipientName(e.target.value)}
        error={fieldErrors.recipientName}
        required
      />
      <Input
        label="Số điện thoại"
        value={phoneNumber}
        onChange={(e) => setPhoneNumber(e.target.value)}
        error={fieldErrors.phoneNumber}
        required
      />
      <VietnamAddressFields
        value={vnAddress}
        onChange={setVnAddress}
        errors={fieldErrors}
      />
      <Input label="Ghi chú" value={note} onChange={(e) => setNote(e.target.value)} />
      <Select
        label="Loại địa chỉ"
        value={addressType}
        onChange={(e) => setAddressType(e.target.value as AddressType)}
      >
        <option value="Home">Nhà</option>
        <option value="Office">Văn phòng</option>
      </Select>
      <label className="flex items-center gap-2 text-sm text-ink">
        <input
          type="checkbox"
          checked={isDefault}
          onChange={(e) => setIsDefault(e.target.checked)}
          className="accent-forest"
        />
        Đặt làm mặc định
      </label>
      {error && <p className="text-sm text-rose-700">{error}</p>}
      <div className="flex gap-3">
        <Button type="submit" disabled={saving}>
          {saving ? 'Đang lưu...' : 'Lưu'}
        </Button>
        <Button type="button" variant="ghost" onClick={onCancel}>
          Hủy
        </Button>
      </div>
    </form>
  )
}

function BankAccountsTab() {
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState<UserBankAccount | null>(null)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState('')

  const query = useQuery({
    queryKey: ['my-bank-accounts'],
    queryFn: () => bankAccountsApi.list(),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => bankAccountsApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-bank-accounts'] }),
    onError: (err) => setError(getErrorMessage(err)),
  })

  const defaultMutation = useMutation({
    mutationFn: (id: string) => bankAccountsApi.setDefault(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['my-bank-accounts'] }),
    onError: (err) => setError(getErrorMessage(err)),
  })

  if (query.isLoading) return <Spinner />

  if (creating || editing) {
    return (
      <BankAccountForm
        account={editing}
        onDone={() => {
          setCreating(false)
          setEditing(null)
          queryClient.invalidateQueries({ queryKey: ['my-bank-accounts'] })
        }}
        onCancel={() => {
          setCreating(false)
          setEditing(null)
        }}
      />
    )
  }

  return (
    <div className="space-y-3">
      {error && <p className="text-sm text-rose-700">{error}</p>}
      <Button onClick={() => setCreating(true)}>Thêm tài khoản</Button>
      {query.data?.length === 0 && <EmptyState title="Chưa có tài khoản ngân hàng" />}
      {query.data?.map((acc) => (
        <div
          key={acc.id}
          className="flex flex-col gap-3 rounded-2xl border border-line bg-white/80 p-4 sm:flex-row sm:items-center"
        >
          <div className="flex-1 space-y-1">
            <p className="font-medium text-ink">{acc.bankName}</p>
            <p className="text-sm text-muted">
              {acc.accountNumberMasked} · {acc.accountHolderName}
            </p>
            {acc.branch && <p className="text-xs text-muted">{acc.branch}</p>}
            <div className="flex gap-2">
              {acc.isDefault && <Badge tone="success">Mặc định</Badge>}
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            {!acc.isDefault && (
              <Button variant="secondary" onClick={() => defaultMutation.mutate(acc.id)}>
                Đặt mặc định
              </Button>
            )}
            <Button variant="secondary" onClick={() => setEditing(acc)}>
              Sửa
            </Button>
            <Button variant="danger" onClick={() => deleteMutation.mutate(acc.id)}>
              Xóa
            </Button>
          </div>
        </div>
      ))}
    </div>
  )
}

function BankAccountForm({
  account,
  onDone,
  onCancel,
}: {
  account: UserBankAccount | null
  onDone: () => void
  onCancel: () => void
}) {
  const [form, setForm] = useState({
    bankName: account?.bankName ?? '',
    accountNumber: account?.accountNumber ?? '',
    accountHolderName: account?.accountHolderName ?? '',
    branch: account?.branch ?? '',
    isDefault: account?.isDefault ?? false,
  })
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      const payload = { ...form, branch: form.branch || undefined }
      if (account) {
        await bankAccountsApi.update(account.id, payload)
      } else {
        await bankAccountsApi.create(payload)
      }
      onDone()
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  const set = (field: string, value: string | boolean) =>
    setForm((f) => ({ ...f, [field]: value }))

  return (
    <form
      className="max-w-lg space-y-4 rounded-2xl border border-line bg-white/80 p-6"
      onSubmit={handleSubmit}
    >
      <h2 className="font-display text-xl text-ink">
        {account ? 'Sửa tài khoản' : 'Thêm tài khoản ngân hàng'}
      </h2>
      <Input
        label="Tên ngân hàng"
        value={form.bankName}
        onChange={(e) => set('bankName', e.target.value)}
        required
      />
      <Input
        label="Số tài khoản"
        value={form.accountNumber}
        onChange={(e) => set('accountNumber', e.target.value)}
        required
      />
      <Input
        label="Tên chủ tài khoản"
        value={form.accountHolderName}
        onChange={(e) => set('accountHolderName', e.target.value)}
        required
      />
      <Input
        label="Chi nhánh"
        value={form.branch}
        onChange={(e) => set('branch', e.target.value)}
      />
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={form.isDefault}
          onChange={(e) => set('isDefault', e.target.checked)}
          className="accent-forest"
        />
        Đặt làm mặc định
      </label>
      {error && <p className="text-sm text-rose-700">{error}</p>}
      <div className="flex gap-3">
        <Button type="submit" disabled={saving}>
          {saving ? 'Đang lưu...' : 'Lưu'}
        </Button>
        <Button type="button" variant="ghost" onClick={onCancel}>
          Hủy
        </Button>
      </div>
    </form>
  )
}
