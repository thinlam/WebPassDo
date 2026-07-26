import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { locationsApi } from '../../features/locations/api'

export type VietnamAddressValue = {
  provinceCode: string
  provinceName: string
  districtCode: string
  districtName: string
  wardCode: string
  wardName: string
  addressLine: string
}

type Props = {
  value: VietnamAddressValue
  onChange: (value: VietnamAddressValue) => void
  errors?: Partial<Record<keyof VietnamAddressValue, string>>
  disabled?: boolean
}

export function VietnamAddressFields({ value, onChange, errors, disabled }: Props) {
  const [provinceSearch, setProvinceSearch] = useState('')
  const [districtSearch, setDistrictSearch] = useState('')
  const [wardSearch, setWardSearch] = useState('')

  const provincesQuery = useQuery({
    queryKey: ['locations', 'provinces'],
    queryFn: () => locationsApi.provinces(),
    staleTime: 24 * 60 * 60 * 1000,
  })

  const districtsQuery = useQuery({
    queryKey: ['locations', 'districts', value.provinceCode],
    queryFn: () => locationsApi.districts(value.provinceCode),
    enabled: !!value.provinceCode,
    staleTime: 24 * 60 * 60 * 1000,
  })

  const wardsQuery = useQuery({
    queryKey: ['locations', 'wards', value.districtCode],
    queryFn: () => locationsApi.wards(value.districtCode),
    enabled: !!value.districtCode,
    staleTime: 24 * 60 * 60 * 1000,
  })

  useEffect(() => {
    setProvinceSearch('')
    setDistrictSearch('')
    setWardSearch('')
  }, [value.provinceCode, value.districtCode, value.wardCode])

  const provinces = useMemo(() => {
    const items = provincesQuery.data ?? []
    const q = provinceSearch.trim().toLowerCase()
    if (!q) return items
    return items.filter((x) => x.name.toLowerCase().includes(q))
  }, [provincesQuery.data, provinceSearch])

  const districts = useMemo(() => {
    const items = districtsQuery.data ?? []
    const q = districtSearch.trim().toLowerCase()
    if (!q) return items
    return items.filter((x) => x.name.toLowerCase().includes(q))
  }, [districtsQuery.data, districtSearch])

  const wards = useMemo(() => {
    const items = wardsQuery.data ?? []
    const q = wardSearch.trim().toLowerCase()
    if (!q) return items
    return items.filter((x) => x.name.toLowerCase().includes(q))
  }, [wardsQuery.data, wardSearch])

  return (
    <div className="space-y-4">
      <SearchableSelect
        label="Tỉnh/Thành phố"
        placeholder={provincesQuery.isLoading ? 'Đang tải...' : 'Chọn tỉnh/thành phố'}
        search={provinceSearch}
        onSearchChange={setProvinceSearch}
        options={provinces}
        value={value.provinceCode}
        fallbackLabel={value.provinceName}
        disabled={disabled || provincesQuery.isLoading}
        error={errors?.provinceName || errors?.provinceCode}
        onSelect={(item) =>
          onChange({
            ...value,
            provinceCode: item.code,
            provinceName: item.name,
            districtCode: '',
            districtName: '',
            wardCode: '',
            wardName: '',
          })
        }
      />

      <SearchableSelect
        label="Quận/Huyện"
        placeholder={
          !value.provinceCode
            ? 'Chọn tỉnh trước'
            : districtsQuery.isLoading
              ? 'Đang tải...'
              : 'Chọn quận/huyện'
        }
        search={districtSearch}
        onSearchChange={setDistrictSearch}
        options={districts}
        value={value.districtCode}
        fallbackLabel={value.districtName}
        disabled={disabled || !value.provinceCode || districtsQuery.isLoading}
        error={errors?.districtName || errors?.districtCode}
        onSelect={(item) =>
          onChange({
            ...value,
            districtCode: item.code,
            districtName: item.name,
            wardCode: '',
            wardName: '',
          })
        }
      />

      <SearchableSelect
        label="Phường/Xã"
        placeholder={
          !value.districtCode
            ? 'Chọn quận/huyện trước'
            : wardsQuery.isLoading
              ? 'Đang tải...'
              : 'Chọn phường/xã'
        }
        search={wardSearch}
        onSearchChange={setWardSearch}
        options={wards}
        value={value.wardCode}
        fallbackLabel={value.wardName}
        disabled={disabled || !value.districtCode || wardsQuery.isLoading}
        error={errors?.wardName || errors?.wardCode}
        onSelect={(item) =>
          onChange({
            ...value,
            wardCode: item.code,
            wardName: item.name,
          })
        }
      />

      <label className="block space-y-1.5">
        <span className="text-sm font-medium text-ink">Số nhà, tên đường, tòa nhà...</span>
        <input
          className="w-full rounded-md border border-line bg-surface px-3 py-2 text-sm text-ink outline-none ring-forest/30 focus:ring-2"
          value={value.addressLine}
          disabled={disabled}
          onChange={(e) => onChange({ ...value, addressLine: e.target.value })}
          placeholder="Ví dụ: 12 Nguyễn Huệ, căn hộ 5A"
        />
        {errors?.addressLine && <span className="text-xs text-rose-700">{errors.addressLine}</span>}
      </label>
    </div>
  )
}

function SearchableSelect({
  label,
  placeholder,
  options,
  value,
  fallbackLabel,
  search,
  onSearchChange,
  onSelect,
  disabled,
  error,
}: {
  label: string
  placeholder: string
  options: { code: string; name: string }[]
  value: string
  fallbackLabel?: string
  search: string
  onSearchChange: (v: string) => void
  onSelect: (item: { code: string; name: string }) => void
  disabled?: boolean
  error?: string
}) {
  const [open, setOpen] = useState(false)
  const selected = options.find((x) => x.code === value)
  const displayName = selected?.name || fallbackLabel

  return (
    <label className="relative block space-y-1.5">
      <span className="text-sm font-medium text-ink">{label}</span>
      <button
        type="button"
        disabled={disabled}
        onClick={() => setOpen((v) => !v)}
        className="flex w-full items-center justify-between rounded-md border border-line bg-surface px-3 py-2 text-left text-sm text-ink outline-none ring-forest/30 focus:ring-2 disabled:cursor-not-allowed disabled:opacity-50"
      >
        <span className={displayName ? 'text-ink' : 'text-muted'}>{displayName || placeholder}</span>
        <svg className="h-4 w-4 text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
        </svg>
      </button>
      {open && !disabled && (
        <div className="absolute z-40 mt-1 w-full overflow-hidden rounded-xl border border-line bg-surface shadow-lg">
          <input
            autoFocus
            className="w-full border-b border-line bg-surface px-3 py-2 text-sm text-ink outline-none"
            placeholder="Tìm kiếm..."
            value={search}
            onChange={(e) => onSearchChange(e.target.value)}
          />
          <ul className="max-h-48 overflow-y-auto">
            {options.length === 0 && (
              <li className="px-3 py-2 text-sm text-muted">Không có kết quả</li>
            )}
            {options.map((item) => (
              <li key={item.code}>
                <button
                  type="button"
                  className={`w-full px-3 py-2 text-left text-sm hover:bg-sand ${
                    item.code === value ? 'bg-sand font-medium text-forest' : 'text-ink'
                  }`}
                  onClick={() => {
                    onSelect(item)
                    setOpen(false)
                    onSearchChange('')
                  }}
                >
                  {item.name}
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}
      {error && <span className="text-xs text-rose-700">{error}</span>}
    </label>
  )
}
