import { useTranslation } from 'react-i18next'
import { Badge } from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
interface TenantStatusBadgeProps {
  isActive: boolean
}

export const TenantStatusBadge = ({ isActive }: TenantStatusBadgeProps) => {
  const { t } = useTranslation('common')

  return (
    <Badge variant="outline" className={getStatusBadgeClasses(isActive ? 'green' : 'gray')}>
      {isActive ? t('labels.active') : t('labels.inactive')}
    </Badge>
  )
}
