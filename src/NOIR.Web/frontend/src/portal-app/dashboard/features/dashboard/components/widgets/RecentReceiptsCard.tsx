import { useTranslation } from 'react-i18next'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { ClipboardList } from 'lucide-react'
import { Badge, Card, CardContent, CardHeader, CardTitle, EmptyState } from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import type { RecentReceiptDto } from '@/services/dashboard'

interface RecentReceiptsCardProps {
  receipts: RecentReceiptDto[]
}

export const RecentReceiptsCard = ({ receipts }: RecentReceiptsCardProps) => {
  const { t } = useTranslation('common')
  const { formatRelativeTime } = useRegionalSettings()

  return (
    <Card className="gap-0 shadow-sm hover:shadow-lg transition-all duration-300">
      <CardHeader className="pb-3">
        <CardTitle className="text-lg flex items-center gap-2">
          <ClipboardList className="h-5 w-5 text-primary" />
          {t('dashboard.recentReceipts')}
        </CardTitle>
      </CardHeader>
      <CardContent>
        {receipts.length === 0 ? (
          <EmptyState
            icon={ClipboardList}
            title={t('labels.noData', 'No data available')}
            size="sm"
            className="border-0 rounded-none py-6"
          />
        ) : (
          <div className="space-y-3">
            {receipts.map((receipt) => (
              <div key={receipt.receiptId} className="flex items-center justify-between p-3 rounded-lg border border-border/50">
                <div className="min-w-0">
                  <p className="text-sm font-medium">{receipt.receiptNumber}</p>
                  <p className="text-xs text-muted-foreground">
                    {receipt.itemCount} {t('labels.items', 'items')} &middot; {formatRelativeTime(receipt.date)}
                  </p>
                </div>
                <Badge
                  variant="outline"
                  className={getStatusBadgeClasses(receipt.type === 'StockIn' ? 'green' : 'blue')}
                >
                  {receipt.type === 'StockIn' ? t('dashboard.stockIn') : t('dashboard.stockOut')}
                </Badge>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
