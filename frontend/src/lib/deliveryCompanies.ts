/** Đơn vị vận chuyển phổ biến VN — hand-over thủ công (PASSDO-05). */
export const DELIVERY_COMPANIES = [
  'GHN',
  'GHTK',
  'J&T Express',
  'Viettel Post',
  'SPX Express',
  'Vietnam Post',
  'Ninja Van',
  'Best Express',
] as const

export type DeliveryCompanyPreset = (typeof DELIVERY_COMPANIES)[number]

/** Giá trị select khi seller nhập tay đơn vị khác. */
export const OTHER_DELIVERY_COMPANY = '__other__'
