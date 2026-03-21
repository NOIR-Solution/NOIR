import { useTranslation } from 'react-i18next'
import { ShoppingCart, Star, AlertTriangle, FileText } from 'lucide-react'
import { Badge, Card, CardContent, CardHeader, CardTitle } from '@uikit'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import type { QuickActionCountsDto } from '@/services/dashboard'

interface QuickActionsCardProps {
  counts: QuickActionCountsDto
}

export const QuickActionsCard = ({ counts }: QuickActionsCardProps) => {
  const { t } = useTranslation('common')

  const actions = [
    {
      label: t('dashboard.pendingOrders'),
      count: counts.pendingOrders,
      icon: ShoppingCart,
      href: '/portal/ecommerce/orders',
      color: 'text-blue-600',
      bg: 'bg-blue-100 dark:bg-blue-900/30',
    },
    {
      label: t('dashboard.pendingReviews'),
      count: counts.pendingReviews,
      icon: Star,
      href: '/portal/ecommerce/reviews',
      color: 'text-amber-600',
      bg: 'bg-amber-100 dark:bg-amber-900/30',
    },
    {
      label: t('dashboard.lowStockAlerts'),
      count: counts.lowStockAlerts,
      icon: AlertTriangle,
      href: '/portal/ecommerce/inventory',
      color: 'text-red-600',
      bg: 'bg-red-100 dark:bg-red-900/30',
    },
    {
      label: t('dashboard.draftProducts'),
      count: counts.draftProducts,
      icon: FileText,
      href: '/portal/ecommerce/products',
      color: 'text-violet-600',
      bg: 'bg-violet-100 dark:bg-violet-900/30',
    },
  ]

  return (
    <Card className="gap-0 shadow-sm hover:shadow-lg transition-all duration-300">
      <CardHeader className="pb-3">
        <CardTitle className="text-lg">{t('dashboard.quickActions')}</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-2 gap-3">
          {actions.map((action) => (
            <ViewTransitionLink
              key={action.href}
              to={action.href}
              className="flex items-center gap-3 p-3 rounded-lg border border-border/50 hover:bg-muted/50 transition-colors cursor-pointer"
            >
              <div className={`p-2 rounded-lg ${action.bg}`}>
                <action.icon className={`h-4 w-4 ${action.color}`} />
              </div>
              <div className="min-w-0">
                <p className="text-sm font-medium leading-tight">{action.label}</p>
                <Badge variant="outline" className="text-xs mt-0.5">
                  {action.count}
                </Badge>
              </div>
            </ViewTransitionLink>
          ))}
        </div>
      </CardContent>
    </Card>
  )
}
