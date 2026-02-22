import { useState, useEffect, useDeferredValue, useMemo, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  CreditCard,
  Eye,
  Plus,
  Search,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  Input,
  PageHeader,
  Pagination,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'
import { usePaymentsQuery } from '@/portal-app/payments/queries'
import type { GetPaymentsParams, PaymentStatus, PaymentMethod, PaymentTransactionListDto } from '@/services/payments'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { paymentStatusColors } from '../../utils/paymentStatus'
import { RecordManualPaymentDialog } from '../../components/RecordManualPaymentDialog'

const PAYMENT_STATUSES: PaymentStatus[] = [
  'Pending', 'Processing', 'RequiresAction', 'Authorized', 'Paid',
  'Failed', 'Cancelled', 'Expired', 'Refunded', 'PartialRefund',
  'CodPending', 'CodCollected',
]

const PAYMENT_METHODS: PaymentMethod[] = [
  'EWallet', 'QRCode', 'BankTransfer', 'CreditCard', 'DebitCard',
  'Installment', 'COD', 'BuyNowPayLater',
]

export const PaymentsPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Payments')

  const [recordDialogOpen, setRecordDialogOpen] = useState(false)
  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [methodFilter, setMethodFilter] = useState<string>('all')
  const [isFilterPending, startFilterTransition] = useTransition()
  const [params, setParams] = useState<GetPaymentsParams>({ page: 1, pageSize: 20 })

  useEffect(() => {
    setParams(prev => ({ ...prev, page: 1 }))
  }, [deferredSearch])

  const queryParams = useMemo(() => ({
    ...params,
    search: deferredSearch || undefined,
    status: statusFilter !== 'all' ? statusFilter as PaymentStatus : undefined,
    paymentMethod: methodFilter !== 'all' ? methodFilter as PaymentMethod : undefined,
  }), [params, deferredSearch, statusFilter, methodFilter])

  const { data: paymentsResponse, isLoading: loading, error: queryError } = usePaymentsQuery(queryParams)
  const error = queryError?.message ?? null

  const payments = paymentsResponse?.items ?? []
  const totalCount = paymentsResponse?.totalCount ?? 0
  const pageSize = paymentsResponse?.pageSize ?? 20
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const currentPage = params.page ?? 1

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value)
  }

  const handleStatusFilter = (value: string) => {
    startFilterTransition(() => {
      setStatusFilter(value)
      setParams(prev => ({ ...prev, page: 1 }))
    })
  }

  const handleMethodFilter = (value: string) => {
    startFilterTransition(() => {
      setMethodFilter(value)
      setParams(prev => ({ ...prev, page: 1 }))
    })
  }

  const handlePageChange = (page: number) => {
    startFilterTransition(() => {
      setParams(prev => ({ ...prev, page }))
    })
  }

  const handleViewPayment = (payment: PaymentTransactionListDto) => {
    navigate(`/portal/ecommerce/payments/${payment.id}`)
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={CreditCard}
        title={t('payments.title')}
        description={t('payments.description')}
        responsive
        action={
          <Button
            onClick={() => setRecordDialogOpen(true)}
            className="cursor-pointer"
          >
            <Plus className="h-4 w-4 mr-2" />
            {t('payments.recordPayment.title')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('payments.title')}</CardTitle>
              <CardDescription>
                {t('payments.totalCount', { count: totalCount, defaultValue: `${totalCount} payments total` })}
              </CardDescription>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              {/* Search */}
              <div className="relative flex-1 min-w-[200px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('payments.searchPlaceholder')}
                  value={searchInput}
                  onChange={handleSearchChange}
                  className="pl-9 h-9"
                  aria-label={t('payments.searchPlaceholder')}
                />
              </div>
              {/* Status Filter */}
              <Select value={statusFilter} onValueChange={handleStatusFilter}>
                <SelectTrigger className="w-[160px] h-9 cursor-pointer" aria-label={t('payments.filterByStatus')}>
                  <SelectValue placeholder={t('payments.filterByStatus')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">{t('payments.allStatuses')}</SelectItem>
                  {PAYMENT_STATUSES.map((status) => (
                    <SelectItem key={status} value={status} className="cursor-pointer">
                      {t(`payments.statuses.${status}`, status)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {/* Method Filter */}
              <Select value={methodFilter} onValueChange={handleMethodFilter}>
                <SelectTrigger className="w-[160px] h-9 cursor-pointer" aria-label={t('payments.filterByMethod')}>
                  <SelectValue placeholder={t('payments.filterByMethod')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="cursor-pointer">{t('payments.allMethods')}</SelectItem>
                  {PAYMENT_METHODS.map((method) => (
                    <SelectItem key={method} value={method} className="cursor-pointer">
                      {t(`payments.methods.${method}`, method)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10 sticky left-0 z-10 bg-background"></TableHead>
                  <TableHead>{t('payments.transactionNumber')}</TableHead>
                  <TableHead>{t('payments.amount')}</TableHead>
                  <TableHead>{t('payments.status')}</TableHead>
                  <TableHead>{t('payments.provider')}</TableHead>
                  <TableHead>{t('payments.method')}</TableHead>
                  <TableHead>{t('payments.createdAt')}</TableHead>
                  <TableHead>{t('payments.paidAt')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  [...Array(5)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-8 w-8 rounded" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                    </TableRow>
                  ))
                ) : payments.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={8} className="p-0">
                      <EmptyState
                        icon={CreditCard}
                        title={t('payments.noPayments')}
                        description={t('payments.noPaymentsDescription', 'Payment transactions will appear here.')}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  payments.map((payment) => (
                    <TableRow
                      key={payment.id}
                      className="group transition-colors hover:bg-muted/50 cursor-pointer"
                      onClick={() => handleViewPayment(payment)}
                    >
                      <TableCell className="sticky left-0 z-10 bg-background">
                        <Button
                          variant="ghost"
                          size="sm"
                          className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                          aria-label={t('payments.viewPayment', { transactionNumber: payment.transactionNumber, defaultValue: `View payment ${payment.transactionNumber}` })}
                          onClick={(e) => {
                            e.stopPropagation()
                            handleViewPayment(payment)
                          }}
                        >
                          <Eye className="h-4 w-4" />
                        </Button>
                      </TableCell>
                      <TableCell>
                        <span className="font-mono font-medium text-sm">{payment.transactionNumber}</span>
                      </TableCell>
                      <TableCell>
                        <span className="font-medium">
                          {formatCurrency(payment.amount, payment.currency)}
                        </span>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline" className={paymentStatusColors[payment.status]}>
                          {t(`payments.statuses.${payment.status}`, payment.status)}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm capitalize">{payment.provider}</span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm">
                          {t(`payments.methods.${payment.paymentMethod}`, payment.paymentMethod)}
                        </span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">
                          {formatDateTime(payment.createdAt)}
                        </span>
                      </TableCell>
                      <TableCell>
                        <span className="text-sm text-muted-foreground">
                          {payment.paidAt ? formatDateTime(payment.paidAt) : '—'}
                        </span>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              totalItems={totalCount}
              pageSize={params.pageSize || 20}
              onPageChange={handlePageChange}
              showPageSizeSelector={false}
              className="mt-4"
            />
          )}
        </CardContent>
      </Card>

      <RecordManualPaymentDialog
        open={recordDialogOpen}
        onOpenChange={setRecordDialogOpen}
      />
    </div>
  )
}

export default PaymentsPage
