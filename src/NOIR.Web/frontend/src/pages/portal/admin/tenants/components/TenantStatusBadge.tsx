import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'

interface TenantStatusBadgeProps {
  isActive: boolean
}

export function TenantStatusBadge({ isActive }: TenantStatusBadgeProps) {
  const { t } = useTranslation('common')

  return (
    <Badge variant={isActive ? 'default' : 'secondary'}>
      {isActive ? t('labels.active') : t('labels.inactive')}
    </Badge>
  )
}
