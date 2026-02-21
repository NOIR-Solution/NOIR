import { useFeature } from '@/hooks/useFeatures'
import { useTranslation } from 'react-i18next'
import { ShieldOff } from 'lucide-react'

interface FeatureGuardProps {
  feature: string
  children: React.ReactNode
  fallback?: React.ReactNode
}

/** Guards child routes/components by feature availability */
export const FeatureGuard = ({ feature, children, fallback }: FeatureGuardProps) => {
  const { isEnabled, isLoading } = useFeature(feature)
  const { t } = useTranslation('common')

  if (isLoading) {
    return null // Or a skeleton - keep it lightweight
  }

  if (!isEnabled) {
    return fallback ?? (
      <div className="flex flex-col items-center justify-center min-h-[400px] text-center space-y-4">
        <ShieldOff className="h-16 w-16 text-muted-foreground" />
        <h2 className="text-xl font-semibold">{t('featureManagement.moduleNotAvailable')}</h2>
        <p className="text-muted-foreground max-w-md">
          {t('featureManagement.moduleNotAvailableDescription')}
        </p>
      </div>
    )
  }

  return <>{children}</>
}
