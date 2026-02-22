import { useTranslation } from 'react-i18next'
import { CreditCard, ExternalLink } from 'lucide-react'
import { Link } from 'react-router-dom'
import {
  Badge,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Skeleton,
} from '@uikit'
import { useOrderPaymentsQuery } from '@/portal-app/payments/queries'
import { paymentStatusColors } from '@/portal-app/payments/utils/paymentStatus'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'

interface OrderPaymentInfoProps {
  orderId: string
  currency: string
}

export const OrderPaymentInfo = ({ orderId, currency }: OrderPaymentInfoProps) => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()
  const { data: payments, isLoading } = useOrderPaymentsQuery(orderId)

  if (isLoading) {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
        <CardHeader>
          <CardTitle className="text-sm flex items-center gap-2">
            <CreditCard className="h-4 w-4" />
            {t('orders.paymentInfo', 'Payment Information')}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-4 w-2/3" />
        </CardContent>
      </Card>
    )
  }

  if (!payments || payments.length === 0) {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
        <CardHeader>
          <CardTitle className="text-sm flex items-center gap-2">
            <CreditCard className="h-4 w-4" />
            {t('orders.paymentInfo', 'Payment Information')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">{t('orders.noPaymentInfo', 'No payment recorded')}</p>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
      <CardHeader>
        <CardTitle className="text-sm flex items-center gap-2">
          <CreditCard className="h-4 w-4" />
          {t('orders.paymentInfo', 'Payment Information')}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {payments.map((payment, index) => (
          <div key={payment.id} className={`space-y-3 text-sm ${index > 0 ? 'pt-4 border-t' : ''}`}>
            <div className="flex items-center justify-between">
              <p className="font-medium">{t(`payments.methods.${payment.paymentMethod}`, payment.paymentMethod)}</p>
              <Badge variant="outline" className={paymentStatusColors[payment.status]}>
                {t(`payments.statuses.${payment.status}`, payment.status)}
              </Badge>
            </div>
            <div>
              <p className="text-muted-foreground text-xs">{t('payments.amount', 'Amount')}</p>
              <p className="font-medium">{formatCurrency(payment.amount, currency)}</p>
            </div>
            {payment.paidAt && (
              <div>
                <p className="text-muted-foreground text-xs">{t('payments.paidAt', 'Paid At')}</p>
                <p className="font-medium">{formatDateTime(payment.paidAt)}</p>
              </div>
            )}
            {payment.provider && (
              <div>
                <p className="text-muted-foreground text-xs">{t('payments.provider', 'Provider')}</p>
                <p className="font-medium capitalize">{payment.provider}</p>
              </div>
            )}
            <div>
              <p className="text-muted-foreground text-xs">{t('payments.transaction', 'Transaction')}</p>
              <Link
                to={`/portal/ecommerce/payments/${payment.id}`}
                className="font-medium text-primary hover:text-primary/80 inline-flex items-center gap-1.5"
              >
                {payment.transactionNumber}
                <ExternalLink className="h-3 w-3" />
              </Link>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  )
}
