import { apiClient } from './apiClient'

// Types
export type PaymentStatus =
  | 'Pending'
  | 'Processing'
  | 'RequiresAction'
  | 'Authorized'
  | 'Paid'
  | 'Failed'
  | 'Cancelled'
  | 'Expired'
  | 'Refunded'
  | 'PartialRefund'
  | 'CodPending'
  | 'CodCollected'

export type PaymentMethod =
  | 'EWallet'
  | 'QRCode'
  | 'BankTransfer'
  | 'CreditCard'
  | 'DebitCard'
  | 'Installment'
  | 'COD'
  | 'BuyNowPayLater'

export interface PaymentTransactionDto {
  id: string
  transactionNumber: string
  gatewayTransactionId?: string
  paymentGatewayId: string
  provider: string
  orderId?: string
  customerId?: string
  amount: number
  currency: string
  gatewayFee?: number
  netAmount?: number
  status: PaymentStatus
  failureReason?: string
  failureCode?: string
  paymentMethod: PaymentMethod
  paymentMethodDetail?: string
  paidAt?: string
  expiresAt?: string
  codCollectorName?: string
  codCollectedAt?: string
  createdAt: string
  modifiedAt?: string
}

export interface PaymentTransactionListDto {
  id: string
  transactionNumber: string
  provider: string
  amount: number
  currency: string
  status: PaymentStatus
  paymentMethod: PaymentMethod
  paidAt?: string
  createdAt: string
}

export interface PaymentOperationLogDto {
  id: string
  operationType: string
  provider: string
  correlationId: string
  transactionNumber?: string
  requestData?: string
  responseData?: string
  httpStatusCode?: number
  durationMs: number
  success: boolean
  errorCode?: string
  errorMessage?: string
  userId?: string
  createdAt: string
}

export interface WebhookLogDto {
  id: string
  provider: string
  eventType: string
  gatewayEventId?: string
  processingStatus: string
  signatureValid: boolean
  processingError?: string
  createdAt: string
}

export interface RefundDto {
  id: string
  refundNumber: string
  paymentTransactionId: string
  gatewayRefundId?: string
  amount: number
  currency: string
  status: string
  reason: string
  reasonDetail?: string
  requestedBy?: string
  approvedBy?: string
  processedAt?: string
  createdAt: string
}

export interface PaymentDetailsDto {
  transaction: PaymentTransactionDto
  operationLogs: PaymentOperationLogDto[]
  webhookLogs: WebhookLogDto[]
  refunds: RefundDto[]
}

export interface PaymentTimelineEventDto {
  timestamp: string
  eventType: string
  summary: string
  details?: string
  actor?: string
}

export interface GetPaymentsParams {
  page?: number
  pageSize?: number
  status?: PaymentStatus
  paymentMethod?: PaymentMethod
  provider?: string
  search?: string
  fromDate?: string
  toDate?: string
}

export interface PaymentPagedResult {
  items: PaymentTransactionListDto[]
  totalCount: number
  pageIndex: number
  pageSize: number
}

export interface RecordManualPaymentRequest {
  orderId: string
  amount: number
  currency: string
  paymentMethod: PaymentMethod
  referenceNumber?: string
  notes?: string
  paidAt?: string
}

// API Functions
export const getPayments = async (params: GetPaymentsParams = {}): Promise<PaymentPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.status) queryParams.append('status', params.status)
  if (params.paymentMethod) queryParams.append('paymentMethod', params.paymentMethod)
  if (params.provider) queryParams.append('provider', params.provider)
  if (params.search) queryParams.append('search', params.search)
  if (params.fromDate) queryParams.append('fromDate', params.fromDate)
  if (params.toDate) queryParams.append('toDate', params.toDate)
  const query = queryParams.toString()
  return apiClient<PaymentPagedResult>(`/payments${query ? `?${query}` : ''}`)
}

export const getPaymentDetails = async (id: string): Promise<PaymentDetailsDto> => {
  return apiClient<PaymentDetailsDto>(`/payments/${id}/details`)
}

export const getPaymentTimeline = async (id: string): Promise<PaymentTimelineEventDto[]> => {
  return apiClient<PaymentTimelineEventDto[]>(`/payments/${id}/timeline`)
}

export const refreshPaymentStatus = async (id: string): Promise<PaymentTransactionDto> => {
  return apiClient<PaymentTransactionDto>(`/payments/${id}/refresh`, { method: 'POST' })
}

export const recordManualPayment = async (request: RecordManualPaymentRequest): Promise<PaymentTransactionDto> => {
  return apiClient<PaymentTransactionDto>('/payments/manual', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const confirmCodCollection = async (id: string, notes?: string): Promise<PaymentTransactionDto> => {
  return apiClient<PaymentTransactionDto>(`/payments/${id}/cod/confirm`, {
    method: 'POST',
    body: JSON.stringify({ notes }),
  })
}

// Refund types
export type RefundReason = 'CustomerRequest' | 'Defective' | 'WrongItem' | 'NotDelivered' | 'Duplicate' | 'Other'

export interface RequestRefundRequest {
  paymentTransactionId: string
  amount: number
  reason: RefundReason
  notes?: string
}

export interface ApproveRefundRequest {
  notes?: string
}

export interface RejectRefundRequest {
  reason: string
}

// Refund API functions
export const requestRefund = async (request: RequestRefundRequest): Promise<RefundDto> => {
  return apiClient<RefundDto>('/refunds', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const approveRefund = async (id: string, request?: ApproveRefundRequest): Promise<RefundDto> => {
  return apiClient<RefundDto>(`/refunds/${id}/approve`, {
    method: 'POST',
    body: JSON.stringify(request ?? {}),
  })
}

export const rejectRefund = async (id: string, request: RejectRefundRequest): Promise<RefundDto> => {
  return apiClient<RefundDto>(`/refunds/${id}/reject`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}
