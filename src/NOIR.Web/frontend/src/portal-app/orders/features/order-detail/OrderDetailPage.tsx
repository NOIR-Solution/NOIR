import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  Check,
  Clock,
  CreditCard,
  MapPin,
  Package,
  RotateCcw,
  ShoppingCart,
  Truck,
  X as XIcon,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Input,
  Label,
  PageHeader,
  Separator,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Textarea,
  FilePreviewTrigger,
} from '@uikit'
import {
  useOrderQuery,
  useConfirmOrderMutation,
  useShipOrderMutation,
  useDeliverOrderMutation,
  useCompleteOrderMutation,
  useCancelOrderMutation,
  useReturnOrderMutation,
} from '@/portal-app/orders/queries'
import type { OrderStatus, AddressDto } from '@/types/order'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { getOrderStatusColor } from '@/portal-app/orders/utils/orderStatus'
import { OrderNotes } from './OrderNotes'
import { OrderPaymentInfo } from './OrderPaymentInfo'
import { OrderShipmentTracking } from './OrderShipmentTracking'

// Status timeline steps with distinct colors per step
const STATUS_STEPS: { status: OrderStatus; icon: React.ElementType; color: string; bgColor: string; borderColor: string; lineColor: string }[] = [
  { status: 'Pending', icon: Clock, color: 'text-amber-600 dark:text-amber-400', bgColor: 'bg-amber-100 dark:bg-amber-900/40', borderColor: 'border-amber-400 dark:border-amber-600', lineColor: 'from-amber-400 to-blue-400' },
  { status: 'Confirmed', icon: Check, color: 'text-blue-600 dark:text-blue-400', bgColor: 'bg-blue-100 dark:bg-blue-900/40', borderColor: 'border-blue-400 dark:border-blue-600', lineColor: 'from-blue-400 to-indigo-400' },
  { status: 'Processing', icon: Package, color: 'text-indigo-600 dark:text-indigo-400', bgColor: 'bg-indigo-100 dark:bg-indigo-900/40', borderColor: 'border-indigo-400 dark:border-indigo-600', lineColor: 'from-indigo-400 to-violet-400' },
  { status: 'Shipped', icon: Truck, color: 'text-violet-600 dark:text-violet-400', bgColor: 'bg-violet-100 dark:bg-violet-900/40', borderColor: 'border-violet-400 dark:border-violet-600', lineColor: 'from-violet-400 to-emerald-400' },
  { status: 'Delivered', icon: MapPin, color: 'text-emerald-600 dark:text-emerald-400', bgColor: 'bg-emerald-100 dark:bg-emerald-900/40', borderColor: 'border-emerald-400 dark:border-emerald-600', lineColor: 'from-emerald-400 to-green-400' },
  { status: 'Completed', icon: Check, color: 'text-green-600 dark:text-green-400', bgColor: 'bg-green-100 dark:bg-green-900/40', borderColor: 'border-green-400 dark:border-green-600', lineColor: '' },
]

const STATUS_ORDER: Record<OrderStatus, number> = {
  Pending: 0,
  Confirmed: 1,
  Processing: 2,
  Shipped: 3,
  Delivered: 4,
  Completed: 5,
  Cancelled: -1,
  Refunded: -2,
  Returned: -3,
}

// Map order status to timestamp field
const STATUS_TIMESTAMP_MAP: Record<string, string> = {
  Pending: 'createdAt',
  Confirmed: 'confirmedAt',
  Shipped: 'shippedAt',
  Delivered: 'deliveredAt',
  Completed: 'completedAt',
}

const AddressCard = ({ title, address, icon: Icon }: { title: string; address: AddressDto | null | undefined; icon: React.ElementType }) => {
  const { t } = useTranslation('common')

  if (!address) {
    return (
      <Card className="gap-4 py-5">
        <CardHeader>
          <CardTitle className="text-sm flex items-center gap-2">
            <Icon className="h-4 w-4" />
            {title}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">{t('orders.noAddressProvided', 'No address provided')}</p>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="gap-4 py-5">
      <CardHeader>
        <CardTitle className="text-sm flex items-center gap-2">
          <Icon className="h-4 w-4" />
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-1 text-sm">
        <p className="font-medium">{address.fullName}</p>
        <p className="text-muted-foreground">{address.phone}</p>
        <p className="text-muted-foreground">{address.addressLine1}</p>
        {address.addressLine2 && <p className="text-muted-foreground">{address.addressLine2}</p>}
        <p className="text-muted-foreground">
          {[address.ward, address.district, address.province].filter(Boolean).join(', ')}
        </p>
        <p className="text-muted-foreground">{address.country}</p>
        {address.postalCode && <p className="text-muted-foreground">{address.postalCode}</p>}
      </CardContent>
    </Card>
  )
}

export const OrderDetailPage = () => {
  const { t } = useTranslation('common')
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Orders')

  const canWriteOrders = hasPermission(Permissions.OrdersWrite)
  const canManageOrders = hasPermission(Permissions.OrdersManage)

  const { data: order, isLoading, error: queryError } = useOrderQuery(id)

  const confirmMutation = useConfirmOrderMutation()
  const shipMutation = useShipOrderMutation()
  const deliverMutation = useDeliverOrderMutation()
  const completeMutation = useCompleteOrderMutation()
  const cancelMutation = useCancelOrderMutation()
  const returnMutation = useReturnOrderMutation()

  // Ship dialog state
  const [showShipDialog, setShowShipDialog] = useState(false)
  const [trackingNumber, setTrackingNumber] = useState('')
  const [shippingCarrier, setShippingCarrier] = useState('')

  // Cancel dialog state
  const [showCancelDialog, setShowCancelDialog] = useState(false)
  const [cancelReason, setCancelReason] = useState('')

  // Return dialog state
  const [showReturnDialog, setShowReturnDialog] = useState(false)
  const [returnReason, setReturnReason] = useState('')

  const handleConfirm = async () => {
    if (!order) return
    try {
      await confirmMutation.mutateAsync(order.id)
      toast.success(t('orders.confirmSuccess', 'Order confirmed successfully'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('orders.actionError', 'Failed to update order')
      toast.error(message)
    }
  }

  const handleShip = async () => {
    if (!order) return
    try {
      await shipMutation.mutateAsync({
        id: order.id,
        trackingNumber: trackingNumber || undefined,
        carrier: shippingCarrier || undefined,
      })
      toast.success(t('orders.shipSuccess', 'Order shipped successfully'))
      setShowShipDialog(false)
      setTrackingNumber('')
      setShippingCarrier('')
    } catch (err) {
      const message = err instanceof Error ? err.message : t('orders.actionError', 'Failed to update order')
      toast.error(message)
    }
  }

  const handleDeliver = async () => {
    if (!order) return
    try {
      await deliverMutation.mutateAsync(order.id)
      toast.success(t('orders.deliverSuccess', 'Order marked as delivered'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('orders.actionError', 'Failed to update order')
      toast.error(message)
    }
  }

  const handleComplete = async () => {
    if (!order) return
    try {
      await completeMutation.mutateAsync(order.id)
      toast.success(t('orders.completeSuccess', 'Order completed successfully'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('orders.actionError', 'Failed to update order')
      toast.error(message)
    }
  }

  const handleCancel = async () => {
    if (!order) return
    try {
      await cancelMutation.mutateAsync({ id: order.id, reason: cancelReason || undefined })
      toast.success(t('orders.cancelSuccess', 'Order cancelled successfully'))
      setShowCancelDialog(false)
      setCancelReason('')
    } catch (err) {
      const message = err instanceof Error ? err.message : t('orders.actionError', 'Failed to update order')
      toast.error(message)
    }
  }

  const handleReturn = async () => {
    if (!order || !returnReason.trim()) return
    try {
      await returnMutation.mutateAsync({ id: order.id, reason: returnReason })
      toast.success(t('orders.returnSuccess', 'Order return processed successfully'))
      setShowReturnDialog(false)
      setReturnReason('')
    } catch (err) {
      const message = err instanceof Error ? err.message : t('orders.actionError', 'Failed to update order')
      toast.error(message)
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10 rounded" />
          <div className="space-y-2">
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-72" />
          </div>
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <Skeleton className="h-64 w-full rounded-lg" />
            <Skeleton className="h-48 w-full rounded-lg" />
          </div>
          <div className="space-y-6">
            <Skeleton className="h-48 w-full rounded-lg" />
            <Skeleton className="h-48 w-full rounded-lg" />
          </div>
        </div>
      </div>
    )
  }

  if (queryError || !order) {
    return (
      <div className="space-y-6">
        <Button variant="ghost" onClick={() => navigate('/portal/ecommerce/orders')} className="cursor-pointer">
          <ArrowLeft className="h-4 w-4 mr-2" />
          {t('orders.backToOrders', 'Back to Orders')}
        </Button>
        <div className="p-8 text-center">
          <p className="text-destructive">{queryError?.message || t('orders.orderNotFound', 'Order not found')}</p>
        </div>
      </div>
    )
  }

  const currentStatusOrder = STATUS_ORDER[order.status]
  const isTerminalStatus = currentStatusOrder < 0

  // Determine available actions
  const canConfirm = canWriteOrders && order.status === 'Pending'
  const canShip = canWriteOrders && (order.status === 'Confirmed' || order.status === 'Processing')
  const canDeliver = canWriteOrders && order.status === 'Shipped'
  const canComplete = canWriteOrders && order.status === 'Delivered'
  const canCancel = canWriteOrders && ['Pending', 'Confirmed', 'Processing'].includes(order.status)
  const canReturn = canManageOrders && order.status === 'Delivered'

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate('/portal/ecommerce/orders')} className="cursor-pointer" aria-label={t('orders.backToOrders', 'Back to Orders')}>
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <PageHeader
          icon={ShoppingCart}
          title={`${t('orders.order', 'Order')} ${order.orderNumber}`}
          description={t('orders.orderCreatedAt', { date: formatDateTime(order.createdAt), defaultValue: `Created ${formatDateTime(order.createdAt)}` })}
          responsive
          action={
            <Badge variant="outline" className={`text-sm px-3 py-1 ${getOrderStatusColor(order.status)}`}>
              {t(`orders.status.${order.status.toLowerCase()}`, order.status)}
            </Badge>
          }
        />
      </div>

      {/* Status Timeline - Full Width */}
      {!isTerminalStatus && (
        <Card className="shadow-sm py-6 px-2">
          <CardContent className="px-4">
            {/* Icons row */}
            <div className="relative flex items-center justify-between">
              {/* Connecting lines (positioned behind icons) */}
              <div className="absolute inset-x-0 top-6 flex px-6">
                {STATUS_STEPS.slice(0, -1).map((step) => {
                  const stepOrder = STATUS_ORDER[step.status]
                  return (
                    <div key={`line-${step.status}`} className="flex-1 px-3">
                      <div className={`h-1 rounded-full w-full transition-all duration-500 ${
                        currentStatusOrder > stepOrder
                          ? `bg-gradient-to-r ${step.lineColor}`
                          : 'bg-muted-foreground/10'
                      }`} />
                    </div>
                  )
                })}
              </div>

              {/* Step nodes */}
              {STATUS_STEPS.map((step) => {
                const stepOrder = STATUS_ORDER[step.status]
                const isCompleted = currentStatusOrder >= stepOrder
                const isCurrent = currentStatusOrder === stepOrder
                const StepIcon = step.icon
                const timestampField = STATUS_TIMESTAMP_MAP[step.status] as keyof typeof order | undefined
                const timestamp = timestampField ? (order[timestampField] as string | undefined) : undefined

                return (
                  <div key={step.status} className="relative z-10 flex flex-col items-center gap-2">
                    <div
                      className={`h-12 w-12 rounded-full flex items-center justify-center border-2 transition-all duration-500 ${
                        isCompleted
                          ? `${step.bgColor} ${step.borderColor} ${step.color}`
                          : isCurrent
                            ? `${step.bgColor} ${step.borderColor} ${step.color} animate-pulse ring-4 ring-offset-2 ring-offset-background ring-current/20`
                            : 'bg-muted border-muted-foreground/20 text-muted-foreground/30'
                      }`}
                    >
                      <StepIcon className="h-5 w-5" />
                    </div>
                    <span className={`text-xs text-center font-medium whitespace-nowrap ${
                      isCompleted || isCurrent ? 'text-foreground' : 'text-muted-foreground/40'
                    }`}>
                      {t(`orders.status.${step.status.toLowerCase()}`, step.status)}
                    </span>
                    {isCompleted && timestamp ? (
                      <span className="text-[10px] text-muted-foreground text-center leading-tight whitespace-nowrap">
                        {formatDateTime(timestamp)}
                      </span>
                    ) : (
                      <span className="text-[10px] invisible">-</span>
                    )}
                  </div>
                )
              })}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Terminal status banner */}
      {isTerminalStatus && (
        <Card className={order.status === 'Refunded' ? 'border-orange-300 bg-orange-50 dark:border-orange-700 dark:bg-orange-950/30' : 'border-destructive/30 bg-destructive/5'}>
          <CardContent className="py-4">
            <div className="flex items-center gap-3">
              <div className={order.status === 'Refunded'
                ? 'p-2 rounded-xl bg-orange-100 border border-orange-200 dark:bg-orange-900/30 dark:border-orange-700'
                : 'p-2 rounded-xl bg-destructive/10 border border-destructive/20'
              }>
                {order.status === 'Refunded' ? (
                  <CreditCard className="h-5 w-5 text-orange-600 dark:text-orange-400" />
                ) : order.status === 'Returned' ? (
                  <RotateCcw className="h-5 w-5 text-destructive" />
                ) : (
                  <XIcon className="h-5 w-5 text-destructive" />
                )}
              </div>
              <div>
                <p className={order.status === 'Refunded' ? 'font-medium text-orange-700 dark:text-orange-400' : 'font-medium text-destructive'}>
                  {t(`orders.status.${order.status.toLowerCase()}`, order.status)}
                </p>
                {order.status === 'Refunded' && (
                  <p className="text-sm text-muted-foreground mt-0.5">
                    {t('orders.refundedDescription', 'This order has been refunded to the customer.')}
                  </p>
                )}
                {order.cancellationReason && (
                  <p className="text-sm text-muted-foreground mt-0.5">
                    {t('orders.reason', 'Reason')}: {order.cancellationReason}
                  </p>
                )}
                {order.returnReason && (
                  <p className="text-sm text-muted-foreground mt-0.5">
                    {t('orders.reason', 'Reason')}: {order.returnReason}
                  </p>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left Column - Items and Actions */}
        <div className="lg:col-span-2 space-y-6">
          {/* Order Items */}
          <Card className="shadow-sm gap-4 py-5">
            <CardHeader>
              <CardTitle className="text-sm flex items-center gap-2">
                <Package className="h-4 w-4" />
                {t('orders.orderItems', 'Order Items')}
              </CardTitle>
              <CardDescription>
                {t('orders.itemsCount', { count: order.items.length, defaultValue: `${order.items.length} items` })}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="rounded-lg border overflow-hidden">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>{t('labels.product', 'Product')}</TableHead>
                      <TableHead>{t('labels.sku', 'SKU')}</TableHead>
                      <TableHead className="text-center">{t('labels.quantity', 'Qty')}</TableHead>
                      <TableHead className="text-right">{t('orders.unitPrice', 'Unit Price')}</TableHead>
                      <TableHead className="text-right">{t('orders.lineTotal', 'Total')}</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {order.items.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell>
                          <div className="flex items-center gap-3">
                            <FilePreviewTrigger
                              file={{
                                url: item.imageUrl ?? '',
                                name: item.productName,
                              }}
                              thumbnailWidth={40}
                              thumbnailHeight={40}
                              className="rounded-md"
                            />
                            <div>
                              <p className="font-medium text-sm">{item.productName}</p>
                              {item.variantName && (
                                <p className="text-xs text-muted-foreground">{item.variantName}</p>
                              )}
                            </div>
                          </div>
                        </TableCell>
                        <TableCell>
                          {item.sku ? (
                            <code className="text-xs bg-muted px-1.5 py-0.5 rounded">{item.sku}</code>
                          ) : (
                            <span className="text-muted-foreground">-</span>
                          )}
                        </TableCell>
                        <TableCell className="text-center">{item.quantity}</TableCell>
                        <TableCell className="text-right">
                          {formatCurrency(item.unitPrice, order.currency)}
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {formatCurrency(item.lineTotal, order.currency)}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>

              {/* Order Summary */}
              <div className="mt-4 space-y-2 border-t pt-4">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">{t('orders.subtotal', 'Subtotal')}</span>
                  <span>{formatCurrency(order.subTotal, order.currency)}</span>
                </div>
                {order.discountAmount > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{t('orders.discount', 'Discount')}</span>
                    <span className="text-green-600">-{formatCurrency(order.discountAmount, order.currency)}</span>
                  </div>
                )}
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">{t('orders.shipping', 'Shipping')}</span>
                  <span>{formatCurrency(order.shippingAmount, order.currency)}</span>
                </div>
                {order.taxAmount > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{t('orders.tax', 'Tax')}</span>
                    <span>{formatCurrency(order.taxAmount, order.currency)}</span>
                  </div>
                )}
                {order.couponCode && (
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{t('orders.couponCode', 'Coupon')}</span>
                    <Badge variant="outline" className="text-xs">{order.couponCode}</Badge>
                  </div>
                )}
                <Separator />
                <div className="flex justify-between font-medium text-base">
                  <span>{t('orders.grandTotal', 'Grand Total')}</span>
                  <span>{formatCurrency(order.grandTotal, order.currency)}</span>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Order Actions */}
          {(canConfirm || canShip || canDeliver || canComplete || canCancel || canReturn) && (
            <Card className="shadow-sm gap-4 py-5">
              <CardHeader>
                <CardTitle className="text-sm">{t('orders.orderActions', 'Order Actions')}</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-3">
                  {canConfirm && (
                    <Button
                      className="cursor-pointer"
                      onClick={handleConfirm}
                      disabled={confirmMutation.isPending}
                    >
                      <Check className="h-4 w-4 mr-2" />
                      {t('orders.confirmOrder', 'Confirm Order')}
                    </Button>
                  )}
                  {canShip && (
                    <Button
                      className="cursor-pointer"
                      onClick={() => setShowShipDialog(true)}
                      disabled={shipMutation.isPending}
                    >
                      <Truck className="h-4 w-4 mr-2" />
                      {t('orders.shipOrder', 'Ship Order')}
                    </Button>
                  )}
                  {canDeliver && (
                    <Button
                      className="cursor-pointer"
                      onClick={handleDeliver}
                      disabled={deliverMutation.isPending}
                    >
                      <MapPin className="h-4 w-4 mr-2" />
                      {t('orders.markDelivered', 'Mark Delivered')}
                    </Button>
                  )}
                  {canComplete && (
                    <Button
                      className="cursor-pointer"
                      onClick={handleComplete}
                      disabled={completeMutation.isPending}
                    >
                      <Check className="h-4 w-4 mr-2" />
                      {t('orders.completeOrder', 'Complete Order')}
                    </Button>
                  )}
                  {canCancel && (
                    <Button
                      variant="outline"
                      className="cursor-pointer text-destructive border-destructive/30 hover:bg-destructive/10"
                      onClick={() => setShowCancelDialog(true)}
                      disabled={cancelMutation.isPending}
                    >
                      <XIcon className="h-4 w-4 mr-2" />
                      {t('orders.cancelOrder', 'Cancel Order')}
                    </Button>
                  )}
                  {canReturn && (
                    <Button
                      variant="outline"
                      className="cursor-pointer text-destructive border-destructive/30 hover:bg-destructive/10"
                      onClick={() => setShowReturnDialog(true)}
                      disabled={returnMutation.isPending}
                    >
                      <RotateCcw className="h-4 w-4 mr-2" />
                      {t('orders.returnOrder', 'Return Order')}
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Shipment Tracking */}
          {order.trackingNumber && (
            <OrderShipmentTracking
              orderId={order.id}
              trackingNumber={order.trackingNumber}
              shippingCarrier={order.shippingCarrier}
              currency={order.currency}
            />
          )}

          {/* Internal Notes */}
          <OrderNotes orderId={order.id} canWrite={canWriteOrders} />
        </div>

        {/* Right Column - Customer & Shipping Info */}
        <div className="space-y-6">
          {/* Customer Info */}
          <Card className="shadow-sm gap-4 py-5">
            <CardHeader>
              <CardTitle className="text-sm">{t('orders.customerInfo', 'Customer Information')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              {order.customerName && (
                <div>
                  <p className="text-muted-foreground text-xs">{t('labels.name', 'Name')}</p>
                  <p className="font-medium">{order.customerName}</p>
                </div>
              )}
              <div>
                <p className="text-muted-foreground text-xs">{t('labels.email', 'Email')}</p>
                <p className="font-medium">{order.customerEmail}</p>
              </div>
              {order.customerPhone && (
                <div>
                  <p className="text-muted-foreground text-xs">{t('labels.phone', 'Phone')}</p>
                  <p className="font-medium">{order.customerPhone}</p>
                </div>
              )}
              {order.customerNotes && (
                <div>
                  <p className="text-muted-foreground text-xs">{t('orders.customerNotes', 'Customer Notes')}</p>
                  <p className="text-muted-foreground italic">{order.customerNotes}</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Payment Info */}
          <OrderPaymentInfo orderId={order.id} currency={order.currency} />

          {/* Shipping Info — method only; tracking detail is in OrderShipmentTracking */}
          {order.shippingMethod && (
            <Card className="shadow-sm gap-4 py-5">
              <CardHeader>
                <CardTitle className="text-sm flex items-center gap-2">
                  <Truck className="h-4 w-4" />
                  {t('orders.shippingInfo', 'Shipping Information')}
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm">
                <div>
                  <p className="text-muted-foreground text-xs">{t('orders.shippingMethod', 'Shipping Method')}</p>
                  <p className="font-medium">{order.shippingMethod}</p>
                </div>
                {order.estimatedDeliveryAt && (
                  <div>
                    <p className="text-muted-foreground text-xs">{t('orders.estimatedDelivery', 'Estimated Delivery')}</p>
                    <p className="font-medium">{formatDateTime(order.estimatedDeliveryAt)}</p>
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {/* Addresses */}
          <AddressCard
            title={t('orders.shippingAddress', 'Shipping Address')}
            address={order.shippingAddress}
            icon={MapPin}
          />
          <AddressCard
            title={t('orders.billingAddress', 'Billing Address')}
            address={order.billingAddress}
            icon={CreditCard}
          />

          {/* Order Timestamps */}
          <Card className="shadow-sm gap-4 py-5">
            <CardHeader>
              <CardTitle className="text-sm">{t('orders.timeline', 'Timeline')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('orders.created', 'Created')}</span>
                <span>{formatDateTime(order.createdAt)}</span>
              </div>
              {order.confirmedAt && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('orders.status.confirmed', 'Confirmed')}</span>
                  <span>{formatDateTime(order.confirmedAt)}</span>
                </div>
              )}
              {order.shippedAt && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('orders.status.shipped', 'Shipped')}</span>
                  <span>{formatDateTime(order.shippedAt)}</span>
                </div>
              )}
              {order.deliveredAt && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('orders.status.delivered', 'Delivered')}</span>
                  <span>{formatDateTime(order.deliveredAt)}</span>
                </div>
              )}
              {order.completedAt && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('orders.status.completed', 'Completed')}</span>
                  <span>{formatDateTime(order.completedAt)}</span>
                </div>
              )}
              {order.cancelledAt && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('orders.status.cancelled', 'Cancelled')}</span>
                  <span>{formatDateTime(order.cancelledAt)}</span>
                </div>
              )}
              {order.returnedAt && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('orders.status.returned', 'Returned')}</span>
                  <span>{formatDateTime(order.returnedAt)}</span>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Ship Order Dialog */}
      <AlertDialog open={showShipDialog} onOpenChange={setShowShipDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
                <Truck className="h-5 w-5 text-primary" />
              </div>
              <div>
                <AlertDialogTitle>{t('orders.shipOrderTitle', 'Ship Order')}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t('orders.shipOrderDescription', 'Enter tracking information for this shipment.')}
                </AlertDialogDescription>
              </div>
            </div>
          </AlertDialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="trackingNumber">{t('orders.trackingNumber', 'Tracking Number')}</Label>
              <Input
                id="trackingNumber"
                value={trackingNumber}
                onChange={(e) => setTrackingNumber(e.target.value)}
                placeholder={t('orders.trackingNumberPlaceholder', 'Enter tracking number...')}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="shippingCarrier">{t('orders.carrier', 'Carrier')}</Label>
              <Input
                id="shippingCarrier"
                value={shippingCarrier}
                onChange={(e) => setShippingCarrier(e.target.value)}
                placeholder={t('orders.carrierPlaceholder', 'e.g. GHN, GHTK, VNPost...')}
              />
            </div>
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">{t('labels.cancel', 'Cancel')}</AlertDialogCancel>
            <AlertDialogAction onClick={handleShip} className="cursor-pointer">
              <Truck className="h-4 w-4 mr-2" />
              {t('orders.shipOrder', 'Ship Order')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Cancel Order Dialog */}
      <AlertDialog open={showCancelDialog} onOpenChange={setShowCancelDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <XIcon className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <AlertDialogTitle>{t('orders.cancelOrderTitle', 'Cancel Order')}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t('orders.cancelOrderDescription', 'Are you sure you want to cancel this order? This action cannot be undone.')}
                </AlertDialogDescription>
              </div>
            </div>
          </AlertDialogHeader>
          <div className="py-4">
            <div className="space-y-2">
              <Label htmlFor="cancelReason">{t('orders.reasonOptional', 'Reason (optional)')}</Label>
              <Textarea
                id="cancelReason"
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                placeholder={t('orders.cancelReasonPlaceholder', 'Enter cancellation reason...')}
                rows={3}
              />
            </div>
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">{t('labels.cancel', 'Cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleCancel}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {t('orders.cancelOrder', 'Cancel Order')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Return Order Dialog */}
      <AlertDialog open={showReturnDialog} onOpenChange={setShowReturnDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <RotateCcw className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <AlertDialogTitle>{t('orders.returnOrderTitle', 'Return Order')}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t('orders.returnOrderDescription', 'Process a return for this order. Inventory will be restocked.')}
                </AlertDialogDescription>
              </div>
            </div>
          </AlertDialogHeader>
          <div className="py-4">
            <div className="space-y-2">
              <Label htmlFor="returnReason">{t('orders.returnReasonLabel', 'Reason')}</Label>
              <Textarea
                id="returnReason"
                value={returnReason}
                onChange={(e) => setReturnReason(e.target.value)}
                placeholder={t('orders.returnReasonPlaceholder', 'Enter return reason...')}
                rows={3}
              />
            </div>
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">{t('labels.cancel', 'Cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleReturn}
              disabled={!returnReason.trim()}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              <RotateCcw className="h-4 w-4 mr-2" />
              {t('orders.processReturn', 'Process Return')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

    </div>
  )
}

export default OrderDetailPage
