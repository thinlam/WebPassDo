export type ApiResponse<T> = {
  success: boolean
  message: string | null
  data: T
  errors: Record<string, string[]> | null
}

export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export type UserRole = 'User' | 'Admin' | 'Shipper'

export type AuthUser = {
  id: string
  email: string
  fullName: string
  phoneNumber?: string | null
  avatarUrl?: string | null
  role: UserRole
}

export type AuthSession = {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: AuthUser
}

export type Category = {
  id: string
  name: string
  description?: string | null
  slug?: string | null
  displayOrder: number
  isActive: boolean
}

export type ProductCondition = 'New' | 'LikeNew' | 'Used' | 'Damaged'
export type ProductStatus = 'Draft' | 'Available' | 'Reserved' | 'Sold' | 'Hidden' | 'Rejected'

export type OrderStatus =
  | 'AwaitingPayment'
  | 'PendingConfirmation'
  | 'AwaitingPickup'
  | 'Shipping'
  | 'Delivered'
  | 'Cancelled'
  | 'DeliveryFailed'
  | 'Returned'
  | 'Refunded'

export type DeliverySpeed = 'Express' | 'SameDay' | 'Standard' | 'Intercity'
export type PaymentMethod = 'BankTransfer' | 'CashOnDelivery'
export type AcceptedPaymentOption = 'BankTransfer' | 'CashOnDelivery' | 'Both'
export type PaymentStatus = 'Unpaid' | 'AwaitingConfirmation' | 'Paid' | 'Refunded'
export type AddressType = 'Home' | 'Office'

export type ProductImage = {
  id: string
  url: string
  fileName: string
  isPrimary: boolean
  displayOrder: number
}

export type ProductListItem = {
  id: string
  name: string
  sellingPrice: number
  condition: ProductCondition | string
  status: ProductStatus | string
  location: string
  quantity: number
  categoryId: string
  categoryName?: string | null
  sellerId: string
  primaryImageUrl?: string | null
  createdAt: string
}

export type Product = {
  id: string
  name: string
  description: string
  originalPrice: number
  sellingPrice: number
  condition: ProductCondition | string
  status: ProductStatus | string
  location: string
  quantity: number
  categoryId: string
  categoryName?: string | null
  sellerId: string
  sellerName?: string | null
  pickupAddressId?: string | null
  bankAccountId?: string | null
  acceptedPaymentOption: AcceptedPaymentOption | string
  allowedDeliverySpeeds: string[]
  hasActiveOrders: boolean
  createdAt: string
  images: ProductImage[]
}

export type Favorite = {
  id: string
  productId: string
  productName: string
  sellingPrice: number
  status: string
  condition: string
  location: string
  primaryImageUrl?: string | null
  createdAt: string
}

export type UserAddress = {
  id: string
  recipientName: string
  phoneNumber: string
  province: string
  district: string
  ward: string
  streetAddress: string
  note?: string | null
  addressType: AddressType | string
  isDefault: boolean
  fullAddress: string
}

export type UserBankAccount = {
  id: string
  bankName: string
  accountNumber: string
  accountNumberMasked: string
  accountHolderName: string
  branch?: string | null
  isDefault: boolean
}

export type OrderListItem = {
  id: string
  orderCode: string
  productId: string
  productName: string
  productImageUrl?: string | null
  quantity: number
  productTotal: number
  shippingFee: number
  grandTotal: number
  status: OrderStatus | string
  paymentMethod: PaymentMethod | string
  paymentStatus: PaymentStatus | string
  deliverySpeed: DeliverySpeed | string
  estimatedDeliveryFrom?: string | null
  estimatedDeliveryTo?: string | null
  createdAt: string
  buyerId: string
  buyerName?: string | null
  sellerId: string
  sellerName?: string | null
  shipperId?: string | null
  shipperName?: string | null
}

export type OrderParty = {
  id: string
  fullName: string
  phoneNumber?: string | null
}

export type OrderAddress = {
  recipientName: string
  phoneNumber?: string | null
  province: string
  district: string
  ward: string
  streetAddress: string
  note?: string | null
  fullAddress: string
}

export type OrderItem = {
  productId: string
  productName: string
  productImageUrl?: string | null
  unitPrice: number
  quantity: number
  lineTotal: number
}

export type OrderPayment = {
  method: string
  status: string
  amount: number
  transferContent?: string | null
  proofImageUrl?: string | null
  confirmedAt?: string | null
}

export type OrderShipment = {
  carrierName?: string | null
  trackingCode?: string | null
  deliverySpeed: string
  senderCity: string
  receiverCity: string
  shippingFee: number
  estimatedDeliveryFrom?: string | null
  estimatedDeliveryTo?: string | null
  sellerHandedOverAt?: string | null
  shipperReceivedAt?: string | null
  deliveredAt?: string | null
  deliveryNote?: string | null
  shipperId?: string | null
  shipperName?: string | null
  shipperPhone?: string | null
}

export type OrderBankSnapshot = {
  bankName: string
  accountNumber: string
  accountNumberMasked: string
  accountHolderName: string
  branch?: string | null
}

export type OrderStatusHistory = {
  oldStatus?: string | null
  newStatus: string
  changedByRole?: string | null
  changedByName?: string | null
  note?: string | null
  createdAt: string
}

export type OrderDetail = OrderListItem & {
  note?: string | null
  cancellationReason?: string | null
  confirmedAt?: string | null
  preparedAt?: string | null
  pickedUpAt?: string | null
  deliveredAt?: string | null
  cancelledAt?: string | null
  updatedAt?: string | null
  seller?: OrderParty | null
  buyer?: OrderParty | null
  shipper?: OrderParty | null
  shippingAddress?: OrderAddress | null
  pickupAddress?: OrderAddress | null
  payment?: OrderPayment | null
  shipment?: OrderShipment | null
  sellerBankAccount?: OrderBankSnapshot | null
  items: OrderItem[]
  statusHistory: OrderStatusHistory[]
}

export type OrderPreview = {
  productId: string
  productName: string
  productImageUrl?: string | null
  unitPrice: number
  quantity: number
  productTotal: number
  shippingFee: number
  grandTotal: number
  deliverySpeed: string
  paymentMethod: string
  etaNote: string
  estimatedDeliveryFromPreview?: string | null
  estimatedDeliveryToPreview?: string | null
  sellerBankAccount?: OrderBankSnapshot | null
  allowedDeliverySpeeds: string[]
  allowedPaymentMethods: string[]
}

export type ProductFilters = {
  page?: number
  pageSize?: number
  keyword?: string
  categoryId?: string
  condition?: ProductCondition
  status?: ProductStatus
  minPrice?: number
  maxPrice?: number
  location?: string
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}
