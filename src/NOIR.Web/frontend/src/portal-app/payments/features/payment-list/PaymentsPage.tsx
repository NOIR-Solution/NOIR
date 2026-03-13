import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { CreditCard, Eye, Plus } from 'lucide-react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef } from '@tanstack/react-table'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useTableParams } from '@/hooks/useTableParams'
import { useServerTable } from '@/hooks/useServerTable'
import { createActionsColumn } from '@/lib/table/columnHelpers'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  DataTable,
  DataTableColumnHeader,
  DataTablePagination,
  DataTableToolbar,
  DropdownMenuItem,
  EmptyState,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'
import { usePaymentsQuery } from '@/portal-app/payments/queries'
import type { PaymentStatus, PaymentMethod, PaymentTransactionListDto } from '@/services/payments'
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

const ch = createColumnHelper<PaymentTransactionListDto>()

export const PaymentsPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Payments')

  const [recordDialogOpen, setRecordDialogOpen] = useState(false)

  const {
    params,
    searchInput,
    setSearchInput,
    isSearchStale,
    isFilterPending,
    setFilter,
    setPage,
    setPageSize,
  } = useTableParams<{ status?: PaymentStatus; paymentMethod?: PaymentMethod }>({ defaultPageSize: 20 })

  const { data: paymentsResponse, isLoading, error: queryError, refetch } = usePaymentsQuery(params)
  const error = queryError?.message ?? null

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'PaymentTransaction',
    onCollectionUpdate: refetch,
  })

  const payments = paymentsResponse?.items ?? []

  const handleStatusFilter = (value: string) => setFilter('status', value === 'all' ? undefined : (value as PaymentStatus))
  const handleMethodFilter = (value: string) => setFilter('paymentMethod', value === 'all' ? undefined : (value as PaymentMethod))

  const handleViewPayment = (payment: PaymentTransactionListDto) => {
    navigate(`/portal/ecommerce/payments/${payment.id}`)
  }

  const columns = useMemo((): ColumnDef<PaymentTransactionListDto, unknown>[] => [
    createActionsColumn<PaymentTransactionListDto>((payment) => (
      <DropdownMenuItem className="cursor-pointer" onClick={() => handleViewPayment(payment)}>
        <Eye className="h-4 w-4 mr-2" />
        {t('labels.viewDetails')}
      </DropdownMenuItem>
    )),
    ch.accessor('transactionNumber', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('payments.transactionNumber')} />,
      meta: { label: t('payments.transactionNumber') },
      enableSorting: false,
      cell: ({ getValue }) => <span className="font-mono font-medium text-sm">{getValue()}</span>,
    }) as ColumnDef<PaymentTransactionListDto, unknown>,
    ch.accessor('amount', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('payments.amount')} />,
      meta: { label: t('payments.amount') },
      enableSorting: false,
      cell: ({ row }) => (
        <span className="font-medium">
          {formatCurrency(row.original.amount, row.original.currency)}
        </span>
      ),
    }) as ColumnDef<PaymentTransactionListDto, unknown>,
    ch.accessor('status', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('payments.status')} />,
      meta: { label: t('payments.status') },
      enableSorting: false,
      cell: ({ row }) => (
        <Badge variant="outline" className={paymentStatusColors[row.original.status]}>
          {t(`payments.statuses.${row.original.status}`, row.original.status)}
        </Badge>
      ),
    }) as ColumnDef<PaymentTransactionListDto, unknown>,
    ch.accessor('provider', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('payments.provider')} />,
      meta: { label: t('payments.provider') },
      enableSorting: false,
      cell: ({ getValue }) => <span className="text-sm capitalize">{getValue()}</span>,
    }) as ColumnDef<PaymentTransactionListDto, unknown>,
    ch.accessor('paymentMethod', {
      id: 'method',
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('payments.method')} />,
      meta: { label: t('payments.method') },
      enableSorting: false,
      cell: ({ row }) => (
        <span className="text-sm">
          {t(`payments.methods.${row.original.paymentMethod}`, row.original.paymentMethod)}
        </span>
      ),
    }) as ColumnDef<PaymentTransactionListDto, unknown>,
    ch.accessor('createdAt', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('payments.createdAt')} />,
      meta: { label: t('payments.createdAt') },
      enableSorting: false,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {formatDateTime(row.original.createdAt)}
        </span>
      ),
    }) as ColumnDef<PaymentTransactionListDto, unknown>,
    ch.accessor('paidAt', {
      header: ({ column }) => <DataTableColumnHeader column={column} title={t('payments.paidAt')} />,
      meta: { label: t('payments.paidAt') },
      enableSorting: false,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {row.original.paidAt ? formatDateTime(row.original.paidAt) : '—'}
        </span>
      ),
    }) as ColumnDef<PaymentTransactionListDto, unknown>,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ], [t])

  const table = useServerTable({
    data: payments,
    columns,
    rowCount: paymentsResponse?.totalCount ?? 0,
    columnVisibilityStorageKey: 'payments',
    state: {
      pagination: { pageIndex: params.page - 1, pageSize: params.pageSize },
      sorting: [],
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function'
        ? updater({ pageIndex: params.page - 1, pageSize: params.pageSize })
        : updater
      if (next.pageIndex !== params.page - 1) setPage(next.pageIndex + 1)
      if (next.pageSize !== params.pageSize) setPageSize(next.pageSize)
    },
    getRowId: (row) => row.id,
  })

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={CreditCard}
        title={t('payments.title')}
        description={t('payments.description')}
        responsive
        action={
          <Button
            onClick={() => setRecordDialogOpen(true)}
            className="group transition-all duration-300 cursor-pointer"
          >
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('payments.recordPayment.title')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-0">
        <CardHeader className="pb-3">
          <div className="space-y-3">
            <div>
              <CardTitle className="text-lg">{t('payments.title')}</CardTitle>
              <CardDescription>
                {paymentsResponse ? t('labels.showingCountOfTotal', { count: payments.length, total: paymentsResponse.totalCount }) : ''}
              </CardDescription>
            </div>
            <DataTableToolbar
              table={table}
              searchInput={searchInput}
              onSearchChange={setSearchInput}
              searchPlaceholder={t('payments.searchPlaceholder')}
              isSearchStale={isSearchStale}
              onResetColumnVisibility={table.resetColumnVisibility}
              filterSlot={
                <>
                  <Select value={params.filters.status ?? 'all'} onValueChange={handleStatusFilter}>
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
                  <Select value={params.filters.paymentMethod ?? 'all'} onValueChange={handleMethodFilter}>
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
                </>
              }
            />
          </div>
        </CardHeader>
        <CardContent className={(isSearchStale || isFilterPending) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {error && (
            <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg">
              {error}
            </div>
          )}

          <DataTable
            table={table}
            isLoading={isLoading}
            isStale={isSearchStale || isFilterPending}
            onRowClick={handleViewPayment}
            emptyState={
              <EmptyState
                icon={CreditCard}
                title={t('payments.noPayments')}
                description={t('payments.noPaymentsDescription', 'Payment transactions will appear here.')}
              />
            }
          />

          <DataTablePagination table={table} showPageSizeSelector={false} />
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
