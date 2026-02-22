import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  Banknote,
  Check,
  Clock,
  CreditCard,
  Globe,
  Loader2,
  RefreshCw,
  RotateCcw,
  ShieldCheck,
  ShieldX,
  ThumbsDown,
  ThumbsUp,
  Webhook,
  X as XIcon,
  Zap,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useUrlTab } from '@/hooks/useUrlTab'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  EmptyState,
  Input,
  Label,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Separator,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  Textarea,
} from '@uikit'
import { usePaymentDetailsQuery, usePaymentTimelineQuery, useRefreshPaymentMutation, useConfirmCodCollectionMutation, useRequestRefundMutation, useApproveRefundMutation, useRejectRefundMutation } from '@/portal-app/payments/queries'
import type { PaymentStatus, PaymentTimelineEventDto, RefundReason } from '@/services/payments'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { JsonViewer } from '../../components/JsonViewer'
import { paymentStatusColors } from '../../utils/paymentStatus'

const TERMINAL_STATUSES: PaymentStatus[] = ['Paid', 'Failed', 'Cancelled', 'Expired', 'Refunded']

const getTimelineEventIcon = (eventType: string) => {
  switch (eventType) {
    case 'StatusChange': return Clock
    case 'ApiCall': return Zap
    case 'Webhook': return Webhook
    case 'Refund': return RotateCcw
    default: return Clock
  }
}

const getTimelineEventColor = (eventType: string, summary: string) => {
  if (summary.toLowerCase().includes('fail') || summary.toLowerCase().includes('error')) {
    return 'text-red-500 bg-red-50 border-red-200 dark:bg-red-900/20 dark:border-red-800'
  }
  if (summary.toLowerCase().includes('success') || summary.toLowerCase().includes('paid')) {
    return 'text-green-500 bg-green-50 border-green-200 dark:bg-green-900/20 dark:border-green-800'
  }
  switch (eventType) {
    case 'StatusChange': return 'text-blue-500 bg-blue-50 border-blue-200 dark:bg-blue-900/20 dark:border-blue-800'
    case 'ApiCall': return 'text-purple-500 bg-purple-50 border-purple-200 dark:bg-purple-900/20 dark:border-purple-800'
    case 'Webhook': return 'text-amber-500 bg-amber-50 border-amber-200 dark:bg-amber-900/20 dark:border-amber-800'
    case 'Refund': return 'text-pink-500 bg-pink-50 border-pink-200 dark:bg-pink-900/20 dark:border-pink-800'
    default: return 'text-gray-500 bg-gray-50 border-gray-200 dark:bg-gray-900/20 dark:border-gray-800'
  }
}

const TimelineEvent = ({ event, formatDateTime }: { event: PaymentTimelineEventDto; formatDateTime: (date: string) => string }) => {
  const Icon = getTimelineEventIcon(event.eventType)
  const colorClass = getTimelineEventColor(event.eventType, event.summary)

  return (
    <div className="flex gap-3 relative">
      <div className="flex flex-col items-center">
        <div className={`h-8 w-8 rounded-full flex items-center justify-center border ${colorClass}`}>
          <Icon className="h-4 w-4" />
        </div>
        <div className="w-px flex-1 bg-border" />
      </div>
      <div className="pb-6 flex-1">
        <div className="flex items-center justify-between gap-2">
          <p className="text-sm font-medium">{event.summary}</p>
          <span className="text-xs text-muted-foreground whitespace-nowrap">
            {formatDateTime(event.timestamp)}
          </span>
        </div>
        {event.actor && (
          <p className="text-xs text-muted-foreground mt-0.5">{event.actor}</p>
        )}
        {event.details && (
          <JsonViewer data={event.details} maxHeight="200px" />
        )}
      </div>
    </div>
  )
}

export const PaymentDetailPage = () => {
  const { t } = useTranslation('common')
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Payments')

  const [codConfirmOpen, setCodConfirmOpen] = useState(false)
  const [codNotes, setCodNotes] = useState('')
  const [requestRefundOpen, setRequestRefundOpen] = useState(false)
  const [refundAmount, setRefundAmount] = useState('')
  const [refundReason, setRefundReason] = useState<RefundReason | ''>('')
  const [refundNotes, setRefundNotes] = useState('')
  const [rejectRefundOpen, setRejectRefundOpen] = useState(false)
  const [rejectRefundId, setRejectRefundId] = useState<string | null>(null)
  const [rejectReason, setRejectReason] = useState('')

  const { data: details, isLoading, error: queryError } = usePaymentDetailsQuery(id)
  const { data: timeline } = usePaymentTimelineQuery(id)
  const refreshMutation = useRefreshPaymentMutation()
  const codConfirmMutation = useConfirmCodCollectionMutation()
  const requestRefundMutation = useRequestRefundMutation()
  const approveRefundMutation = useApproveRefundMutation()
  const rejectRefundMutation = useRejectRefundMutation()

  const { activeTab, handleTabChange, isPending: isTabPending } = useUrlTab({ defaultTab: 'overview' })

  const payment = details?.transaction
  const operationLogs = details?.operationLogs ?? []
  const webhookLogs = details?.webhookLogs ?? []
  const refunds = details?.refunds ?? []

  const isTerminalStatus = payment ? TERMINAL_STATUSES.includes(payment.status) : false
  const isGatewayPayment = payment ? payment.provider !== 'cod' && payment.provider !== 'manual' : false
  const canRefresh = isGatewayPayment && !isTerminalStatus
  const isCodPending = payment?.status === 'CodPending'
  const canRequestRefund = payment?.status === 'Paid' || payment?.status === 'PartialRefund'
  const totalRefunded = refunds
    .filter(r => ['Completed', 'Pending', 'Approved', 'Processing'].includes(r.status))
    .reduce((sum, r) => sum + r.amount, 0)
  const remainingRefundable = (payment?.amount ?? 0) - totalRefunded

  const handleRefresh = async () => {
    if (!id) return
    try {
      await refreshMutation.mutateAsync(id)
      toast.success(t('payments.refreshSuccess'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('payments.refreshError')
      toast.error(message)
    }
  }

  const handleConfirmCod = async () => {
    if (!id) return
    try {
      await codConfirmMutation.mutateAsync({ id, notes: codNotes || undefined })
      toast.success(t('payments.codConfirm.success'))
      setCodConfirmOpen(false)
      setCodNotes('')
    } catch (err) {
      const message = err instanceof Error ? err.message : t('payments.codConfirm.error')
      toast.error(message)
    }
  }

  const handleRequestRefund = async () => {
    if (!id || !refundAmount || !refundReason) return
    try {
      await requestRefundMutation.mutateAsync({
        paymentTransactionId: id,
        amount: parseFloat(refundAmount),
        reason: refundReason as RefundReason,
        notes: refundNotes || undefined,
      })
      toast.success(t('payments.refund.requestSuccess'))
      setRequestRefundOpen(false)
      setRefundAmount('')
      setRefundReason('')
      setRefundNotes('')
    } catch (err) {
      const message = err instanceof Error ? err.message : t('payments.refund.requestError')
      toast.error(message)
    }
  }

  const handleApproveRefund = async (refundId: string) => {
    try {
      await approveRefundMutation.mutateAsync({ id: refundId })
      toast.success(t('payments.refund.approveSuccess'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('payments.refund.approveError')
      toast.error(message)
    }
  }

  const handleRejectRefund = async () => {
    if (!rejectRefundId || !rejectReason) return
    try {
      await rejectRefundMutation.mutateAsync({ id: rejectRefundId, request: { reason: rejectReason } })
      toast.success(t('payments.refund.rejectSuccess'))
      setRejectRefundOpen(false)
      setRejectRefundId(null)
      setRejectReason('')
    } catch (err) {
      const message = err instanceof Error ? err.message : t('payments.refund.rejectError')
      toast.error(message)
    }
  }

  if (isLoading) {
    return (
      <div className="container max-w-6xl py-6 space-y-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10 rounded" />
          <div className="space-y-2">
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-72" />
          </div>
        </div>
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="pt-6">
            <Skeleton className="h-4 w-32 mb-4" />
            <Skeleton className="h-3 w-full mb-2" />
            <Skeleton className="h-3 w-full mb-2" />
            <Skeleton className="h-3 w-3/4" />
          </CardContent>
        </Card>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {[...Array(4)].map((_, i) => (
            <Card key={i} className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6">
                <Skeleton className="h-4 w-24 mb-4" />
                <div className="space-y-3">
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-2/3" />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    )
  }

  if (queryError || !payment) {
    return (
      <div className="container max-w-6xl py-6 space-y-6">
        <Button variant="ghost" onClick={() => navigate('/portal/ecommerce/payments')} className="cursor-pointer">
          <ArrowLeft className="h-4 w-4 mr-2" />
          {t('payments.backToPayments', 'Back to Payments')}
        </Button>
        <div className="p-8 text-center">
          <p className="text-destructive">{queryError?.message || t('payments.paymentNotFound', 'Payment not found')}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="container max-w-6xl py-6 space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => navigate('/portal/ecommerce/payments')}
          className="cursor-pointer"
          aria-label={t('payments.backToPayments', 'Back to Payments')}
        >
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <PageHeader
          icon={CreditCard}
          title={payment.transactionNumber}
          description={t('payments.detail.title')}
          responsive
          action={
            <div className="flex items-center gap-2">
              <Badge variant="outline" className={`text-sm px-3 py-1 ${paymentStatusColors[payment.status]}`}>
                {t(`payments.statuses.${payment.status}`, payment.status)}
              </Badge>
              {isCodPending && (
                <Button
                  variant="default"
                  size="sm"
                  onClick={() => setCodConfirmOpen(true)}
                  className="cursor-pointer"
                  aria-label={t('payments.codConfirm.title')}
                >
                  <Banknote className="h-4 w-4 mr-2" />
                  {t('payments.codConfirm.title')}
                </Button>
              )}
              {canRequestRefund && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setRequestRefundOpen(true)}
                  className="cursor-pointer"
                  aria-label={t('payments.refund.requestRefund')}
                >
                  <RotateCcw className="h-4 w-4 mr-2" />
                  {t('payments.refund.requestRefund')}
                </Button>
              )}
              {canRefresh && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleRefresh}
                  disabled={refreshMutation.isPending}
                  className="cursor-pointer"
                  aria-label={t('payments.refreshFromGateway')}
                >
                  <RefreshCw className={`h-4 w-4 mr-2 ${refreshMutation.isPending ? 'animate-spin' : ''}`} />
                  {refreshMutation.isPending ? t('payments.refreshing') : t('payments.refreshFromGateway')}
                </Button>
              )}
            </div>
          }
        />
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={handleTabChange} className={`w-full${isTabPending ? ' opacity-70 transition-opacity duration-200' : ' transition-opacity duration-200'}`}>
        <TabsList>
          <TabsTrigger value="overview" className="cursor-pointer">
            <CreditCard className="h-4 w-4 mr-2" />
            {t('payments.detail.overview')}
          </TabsTrigger>
          <TabsTrigger value="timeline" className="cursor-pointer">
            <Clock className="h-4 w-4 mr-2" />
            {t('payments.detail.timeline')}
          </TabsTrigger>
          <TabsTrigger value="apiLogs" className="cursor-pointer">
            <Zap className="h-4 w-4 mr-2" />
            {t('payments.detail.apiLogs')}
          </TabsTrigger>
          <TabsTrigger value="webhooks" className="cursor-pointer">
            <Globe className="h-4 w-4 mr-2" />
            {t('payments.detail.webhooks')}
          </TabsTrigger>
          <TabsTrigger value="refunds" className="cursor-pointer">
            <RotateCcw className="h-4 w-4 mr-2" />
            {t('payments.detail.refunds')}
          </TabsTrigger>
        </TabsList>

        {/* Overview Tab */}
        <TabsContent value="overview" className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Transaction Info */}
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
              <CardHeader>
                <CardTitle className="text-sm">{t('payments.detail.transactionInfo')}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('payments.transactionNumber')}</span>
                  <code className="bg-muted px-1.5 py-0.5 rounded text-xs">{payment.transactionNumber}</code>
                </div>
                {payment.gatewayTransactionId && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('payments.detail.gatewayTransactionId', 'Gateway ID')}</span>
                    <code className="bg-muted px-1.5 py-0.5 rounded text-xs">{payment.gatewayTransactionId}</code>
                  </div>
                )}
                {payment.orderId && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('payments.orderNumber')}</span>
                    <Button
                      variant="link"
                      size="sm"
                      className="cursor-pointer h-auto p-0 text-xs"
                      onClick={() => navigate(`/portal/ecommerce/orders/${payment.orderId}`)}
                    >
                      {payment.orderId}
                    </Button>
                  </div>
                )}
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('payments.method')}</span>
                  <span>{t(`payments.methods.${payment.paymentMethod}`, payment.paymentMethod)}</span>
                </div>
                {payment.paymentMethodDetail && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('payments.detail.methodDetail', 'Detail')}</span>
                    <span>{payment.paymentMethodDetail}</span>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Financial */}
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
              <CardHeader>
                <CardTitle className="text-sm">{t('payments.detail.financial')}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('payments.amount')}</span>
                  <span className="font-medium">{formatCurrency(payment.amount, payment.currency)}</span>
                </div>
                {payment.gatewayFee != null && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('payments.detail.gatewayFee', 'Gateway Fee')}</span>
                    <span className="text-orange-600">{formatCurrency(payment.gatewayFee, payment.currency)}</span>
                  </div>
                )}
                {payment.netAmount != null && (
                  <>
                    <Separator />
                    <div className="flex justify-between font-medium">
                      <span>{t('payments.detail.netAmount', 'Net Amount')}</span>
                      <span>{formatCurrency(payment.netAmount, payment.currency)}</span>
                    </div>
                  </>
                )}
              </CardContent>
            </Card>

            {/* Gateway Info */}
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
              <CardHeader>
                <CardTitle className="text-sm">{t('payments.detail.gatewayInfo')}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('payments.provider')}</span>
                  <span className="capitalize">{payment.provider}</span>
                </div>
                {payment.failureReason && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('payments.detail.failureReason', 'Failure Reason')}</span>
                    <span className="text-destructive text-right max-w-[60%]">{payment.failureReason}</span>
                  </div>
                )}
                {payment.failureCode && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('payments.detail.failureCode', 'Failure Code')}</span>
                    <code className="bg-destructive/10 text-destructive px-1.5 py-0.5 rounded text-xs">{payment.failureCode}</code>
                  </div>
                )}
                {payment.codCollectorName && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('payments.detail.codCollector', 'COD Collector')}</span>
                    <span>{payment.codCollectorName}</span>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Timing */}
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
              <CardHeader>
                <CardTitle className="text-sm">{t('payments.detail.timing')}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('payments.createdAt')}</span>
                  <span>{formatDateTime(payment.createdAt)}</span>
                </div>
                {payment.paidAt && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('payments.paidAt')}</span>
                    <span>{formatDateTime(payment.paidAt)}</span>
                  </div>
                )}
                {payment.expiresAt && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('payments.detail.expiresAt', 'Expires At')}</span>
                    <span>{formatDateTime(payment.expiresAt)}</span>
                  </div>
                )}
                {payment.codCollectedAt && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('payments.detail.codCollectedAt', 'COD Collected At')}</span>
                    <span>{formatDateTime(payment.codCollectedAt)}</span>
                  </div>
                )}
                {payment.modifiedAt && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('payments.detail.modifiedAt', 'Last Modified')}</span>
                    <span>{formatDateTime(payment.modifiedAt)}</span>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        {/* Timeline Tab */}
        <TabsContent value="timeline">
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
            <CardHeader>
              <CardTitle className="text-sm">{t('payments.detail.timeline')}</CardTitle>
              <CardDescription>
                {t('payments.detail.timelineDescription', 'Chronological record of all payment events')}
              </CardDescription>
            </CardHeader>
            <CardContent>
              {!timeline || timeline.length === 0 ? (
                <EmptyState
                  icon={Clock}
                  title={t('payments.detail.noTimeline', 'No timeline events')}
                  description={t('payments.detail.noTimelineDescription', 'Timeline events will appear here as the payment progresses.')}
                  className="border-0 px-4 py-8"
                />
              ) : (
                <div className="space-y-0">
                  {timeline.map((event, index) => (
                    <TimelineEvent
                      key={`${event.timestamp}-${index}`}
                      event={event}
                      formatDateTime={formatDateTime}
                    />
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* API Logs Tab */}
        <TabsContent value="apiLogs">
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
            <CardHeader>
              <CardTitle className="text-sm">{t('payments.detail.apiLogs')}</CardTitle>
            </CardHeader>
            <CardContent>
              {operationLogs.length === 0 ? (
                <EmptyState
                  icon={Zap}
                  title={t('payments.detail.noLogs')}
                  description={t('payments.detail.noLogsDescription', 'API operation logs will appear here.')}
                  className="border-0 px-4 py-8"
                />
              ) : (
                <div className="rounded-lg border overflow-hidden">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>{t('payments.detail.operationType')}</TableHead>
                        <TableHead>{t('payments.provider')}</TableHead>
                        <TableHead className="text-right">{t('payments.detail.duration')}</TableHead>
                        <TableHead className="text-center">{t('payments.detail.httpStatus')}</TableHead>
                        <TableHead className="text-center">{t('payments.detail.success')}</TableHead>
                        <TableHead>{t('payments.detail.requestData')}</TableHead>
                        <TableHead>{t('payments.detail.responseData')}</TableHead>
                        <TableHead>{t('payments.createdAt')}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {operationLogs.map((log) => (
                        <TableRow key={log.id}>
                          <TableCell>
                            <span className="font-medium text-sm">{t(`activityTimeline.operations.${log.operationType.toLowerCase()}`, log.operationType)}</span>
                          </TableCell>
                          <TableCell>
                            <span className="text-sm capitalize">{log.provider}</span>
                          </TableCell>
                          <TableCell className="text-right">
                            <span className="font-mono text-xs">{log.durationMs}ms</span>
                          </TableCell>
                          <TableCell className="text-center">
                            {log.httpStatusCode != null ? (
                              <Badge variant="outline" className={
                                log.httpStatusCode < 300
                                  ? 'bg-green-50 text-green-700 dark:bg-green-900/20 dark:text-green-400'
                                  : log.httpStatusCode < 500
                                    ? 'bg-yellow-50 text-yellow-700 dark:bg-yellow-900/20 dark:text-yellow-400'
                                    : 'bg-red-50 text-red-700 dark:bg-red-900/20 dark:text-red-400'
                              }>
                                {log.httpStatusCode}
                              </Badge>
                            ) : (
                              <span className="text-muted-foreground">&mdash;</span>
                            )}
                          </TableCell>
                          <TableCell className="text-center">
                            {log.success ? (
                              <Check className="h-4 w-4 text-green-600 mx-auto" />
                            ) : (
                              <XIcon className="h-4 w-4 text-red-600 mx-auto" />
                            )}
                          </TableCell>
                          <TableCell>
                            <JsonViewer data={log.requestData} maxHeight="200px" />
                          </TableCell>
                          <TableCell>
                            <JsonViewer data={log.responseData} maxHeight="200px" />
                          </TableCell>
                          <TableCell>
                            <span className="text-xs text-muted-foreground">
                              {formatDateTime(log.createdAt)}
                            </span>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Webhooks Tab */}
        <TabsContent value="webhooks">
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
            <CardHeader>
              <CardTitle className="text-sm">{t('payments.detail.webhooks')}</CardTitle>
            </CardHeader>
            <CardContent>
              {webhookLogs.length === 0 ? (
                <EmptyState
                  icon={Globe}
                  title={t('payments.detail.noWebhooks')}
                  description={t('payments.detail.noWebhooksDescription', 'Webhook events will appear here.')}
                  className="border-0 px-4 py-8"
                />
              ) : (
                <div className="rounded-lg border overflow-hidden">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>{t('payments.detail.eventType')}</TableHead>
                        <TableHead>{t('payments.provider')}</TableHead>
                        <TableHead>{t('payments.detail.processingStatus')}</TableHead>
                        <TableHead className="text-center">{t('payments.detail.signatureValid')}</TableHead>
                        <TableHead>{t('payments.createdAt')}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {webhookLogs.map((log) => (
                        <TableRow key={log.id}>
                          <TableCell>
                            <span className="font-medium text-sm">{log.eventType}</span>
                            {log.gatewayEventId && (
                              <p className="text-xs text-muted-foreground mt-0.5">
                                {log.gatewayEventId}
                              </p>
                            )}
                          </TableCell>
                          <TableCell>
                            <span className="text-sm capitalize">{log.provider}</span>
                          </TableCell>
                          <TableCell>
                            <Badge variant="outline" className={
                              log.processingStatus === 'Processed'
                                ? 'bg-green-50 text-green-700 dark:bg-green-900/20 dark:text-green-400'
                                : log.processingStatus === 'Failed'
                                  ? 'bg-red-50 text-red-700 dark:bg-red-900/20 dark:text-red-400'
                                  : 'bg-yellow-50 text-yellow-700 dark:bg-yellow-900/20 dark:text-yellow-400'
                            }>
                              {t(`payments.processingStatus.${log.processingStatus}`, log.processingStatus)}
                            </Badge>
                            {log.processingError && (
                              <p className="text-xs text-destructive mt-1">{log.processingError}</p>
                            )}
                          </TableCell>
                          <TableCell className="text-center">
                            {log.signatureValid ? (
                              <ShieldCheck className="h-4 w-4 text-green-600 mx-auto" />
                            ) : (
                              <ShieldX className="h-4 w-4 text-red-600 mx-auto" />
                            )}
                          </TableCell>
                          <TableCell>
                            <span className="text-xs text-muted-foreground">
                              {formatDateTime(log.createdAt)}
                            </span>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Refunds Tab */}
        <TabsContent value="refunds">
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
            <CardHeader>
              <CardTitle className="text-sm">{t('payments.detail.refunds')}</CardTitle>
            </CardHeader>
            <CardContent>
              {refunds.length === 0 ? (
                <EmptyState
                  icon={RotateCcw}
                  title={t('payments.detail.noRefunds')}
                  description={t('payments.detail.noRefundsDescription', 'Refund records will appear here.')}
                  className="border-0 px-4 py-8"
                />
              ) : (
                <div className="rounded-lg border overflow-hidden">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead className="w-10 sticky left-0 z-10 bg-background"></TableHead>
                        <TableHead>{t('payments.detail.refundNumber', 'Refund #')}</TableHead>
                        <TableHead className="text-right">{t('payments.amount')}</TableHead>
                        <TableHead>{t('payments.status')}</TableHead>
                        <TableHead>{t('payments.detail.reason', 'Reason')}</TableHead>
                        <TableHead>{t('payments.detail.requestedBy', 'Requested By')}</TableHead>
                        <TableHead>{t('payments.createdAt')}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {refunds.map((refund) => (
                        <TableRow key={refund.id}>
                          <TableCell className="sticky left-0 z-10 bg-background">
                            {refund.status === 'Pending' && (
                              <div className="flex items-center gap-1">
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => handleApproveRefund(refund.id)}
                                  disabled={approveRefundMutation.isPending}
                                  className="cursor-pointer h-8 px-2 text-green-600 hover:text-green-700 hover:bg-green-50 dark:hover:bg-green-900/20"
                                  aria-label={t('payments.refund.approve')}
                                >
                                  <ThumbsUp className="h-4 w-4" />
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => { setRejectRefundId(refund.id); setRejectRefundOpen(true) }}
                                  className="cursor-pointer h-8 px-2 text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-900/20"
                                  aria-label={t('payments.refund.reject')}
                                >
                                  <ThumbsDown className="h-4 w-4" />
                                </Button>
                              </div>
                            )}
                          </TableCell>
                          <TableCell>
                            <span className="font-mono font-medium text-sm">{refund.refundNumber}</span>
                          </TableCell>
                          <TableCell className="text-right">
                            <span className="font-medium">{formatCurrency(refund.amount, refund.currency)}</span>
                          </TableCell>
                          <TableCell>
                            <Badge variant="outline" className={
                              refund.status === 'Completed'
                                ? 'bg-green-50 text-green-700 dark:bg-green-900/20 dark:text-green-400'
                                : refund.status === 'Failed'
                                  ? 'bg-red-50 text-red-700 dark:bg-red-900/20 dark:text-red-400'
                                  : 'bg-yellow-50 text-yellow-700 dark:bg-yellow-900/20 dark:text-yellow-400'
                            }>
                              {t(`payments.refundStatus.${refund.status.toLowerCase()}`, refund.status)}
                            </Badge>
                          </TableCell>
                          <TableCell>
                            <span className="text-sm">{refund.reason}</span>
                            {refund.reasonDetail && (
                              <p className="text-xs text-muted-foreground mt-0.5">{refund.reasonDetail}</p>
                            )}
                          </TableCell>
                          <TableCell>
                            <span className="text-sm text-muted-foreground">{refund.requestedBy ?? '—'}</span>
                          </TableCell>
                          <TableCell>
                            <span className="text-xs text-muted-foreground">
                              {formatDateTime(refund.createdAt)}
                            </span>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* COD Collection Confirmation Dialog */}
      <Credenza open={codConfirmOpen} onOpenChange={(open) => { if (!open) { setCodConfirmOpen(false); setCodNotes('') } }}>
        <CredenzaContent>
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
                <Banknote className="h-5 w-5 text-primary" />
              </div>
              <div>
                <CredenzaTitle>{t('payments.codConfirm.title')}</CredenzaTitle>
                <CredenzaDescription>
                  {t('payments.codConfirm.description')}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody>
            <div className="space-y-2">
              <Label htmlFor="codNotes">{t('payments.codConfirm.notes')}</Label>
              <Textarea
                id="codNotes"
                value={codNotes}
                onChange={(e) => setCodNotes(e.target.value)}
                placeholder={t('payments.codConfirm.notesPlaceholder')}
                rows={3}
              />
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => { setCodConfirmOpen(false); setCodNotes('') }} disabled={codConfirmMutation.isPending} className="cursor-pointer">{t('labels.cancel', 'Cancel')}</Button>
            <Button
              onClick={handleConfirmCod}
              disabled={codConfirmMutation.isPending}
              className="cursor-pointer"
            >
              {codConfirmMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t('payments.codConfirm.submit')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Request Refund Dialog */}
      <Credenza open={requestRefundOpen} onOpenChange={(open) => { if (!open) { setRequestRefundOpen(false); setRefundAmount(''); setRefundReason(''); setRefundNotes('') } }}>
        <CredenzaContent className="sm:max-w-[500px]">
          <CredenzaHeader>
            <CredenzaTitle>{t('payments.refund.requestRefund')}</CredenzaTitle>
            <CredenzaDescription>
              {t('payments.refund.requestDescription')}
            </CredenzaDescription>
          </CredenzaHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="refundAmount">{t('payments.amount')}</Label>
              <Input
                id="refundAmount"
                type="number"
                step="0.01"
                min="0.01"
                max={remainingRefundable}
                value={refundAmount}
                onChange={(e) => setRefundAmount(e.target.value)}
                placeholder={t('payments.refund.amountPlaceholder')}
              />
              {remainingRefundable < (payment?.amount ?? 0) && (
                <p className="text-xs text-muted-foreground">
                  {t('payments.refund.maxRefundable')}: {formatCurrency(remainingRefundable, payment?.currency ?? 'VND')}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="refundReason">{t('payments.detail.reason')}</Label>
              <Select value={refundReason} onValueChange={(val) => setRefundReason(val as RefundReason)}>
                <SelectTrigger className="cursor-pointer">
                  <SelectValue placeholder={t('payments.refund.selectReason')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="CustomerRequest" className="cursor-pointer">{t('payments.refund.reasons.CustomerRequest')}</SelectItem>
                  <SelectItem value="Defective" className="cursor-pointer">{t('payments.refund.reasons.Defective')}</SelectItem>
                  <SelectItem value="WrongItem" className="cursor-pointer">{t('payments.refund.reasons.WrongItem')}</SelectItem>
                  <SelectItem value="NotDelivered" className="cursor-pointer">{t('payments.refund.reasons.NotDelivered')}</SelectItem>
                  <SelectItem value="Duplicate" className="cursor-pointer">{t('payments.refund.reasons.Duplicate')}</SelectItem>
                  <SelectItem value="Other" className="cursor-pointer">{t('payments.refund.reasons.Other')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="refundNotes">{t('payments.refund.notes')}</Label>
              <Textarea
                id="refundNotes"
                value={refundNotes}
                onChange={(e) => setRefundNotes(e.target.value)}
                placeholder={t('payments.refund.notesPlaceholder')}
                rows={3}
              />
            </div>
          </div>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setRequestRefundOpen(false)} className="cursor-pointer">
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              onClick={handleRequestRefund}
              disabled={requestRefundMutation.isPending || !refundAmount || !refundReason}
              className="cursor-pointer"
            >
              {requestRefundMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t('payments.refund.submitRequest')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Reject Refund Dialog */}
      <Credenza open={rejectRefundOpen} onOpenChange={(open) => { if (!open) { setRejectRefundOpen(false); setRejectRefundId(null); setRejectReason('') } }}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <ThumbsDown className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('payments.refund.rejectTitle')}</CredenzaTitle>
                <CredenzaDescription>
                  {t('payments.refund.rejectDescription')}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody>
            <div className="space-y-2">
              <Label htmlFor="rejectReason">{t('payments.detail.reason')}</Label>
              <Textarea
                id="rejectReason"
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
                placeholder={t('payments.refund.rejectReasonPlaceholder')}
                rows={3}
              />
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => { setRejectRefundOpen(false); setRejectRefundId(null); setRejectReason('') }} disabled={rejectRefundMutation.isPending} className="cursor-pointer">{t('labels.cancel', 'Cancel')}</Button>
            <Button
              variant="destructive"
              onClick={handleRejectRefund}
              disabled={rejectRefundMutation.isPending || !rejectReason}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {rejectRefundMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t('payments.refund.confirmReject')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default PaymentDetailPage
