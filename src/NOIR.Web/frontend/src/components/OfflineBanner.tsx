import { useTranslation } from 'react-i18next'
import { WifiOff } from 'lucide-react'
import { cn } from '@/lib/utils'

interface OfflineBannerProps {
  visible: boolean
}

export const OfflineBanner = ({ visible }: OfflineBannerProps) => {
  const { t } = useTranslation('common')

  if (!visible) return null

  return (
    <div
      role="alert"
      className={cn(
        'fixed top-0 left-0 right-0 z-[100] flex items-center justify-center gap-2 px-4 py-2.5 text-sm font-medium shadow-md transition-all duration-300 animate-in slide-in-from-top',
        'bg-amber-50 text-amber-800 border-b border-amber-200 dark:bg-amber-950 dark:text-amber-200 dark:border-amber-800',
      )}
    >
      <WifiOff className="h-4 w-4" />
      <span>{t('entityUpdate.offline.banner')}</span>
    </div>
  )
}
