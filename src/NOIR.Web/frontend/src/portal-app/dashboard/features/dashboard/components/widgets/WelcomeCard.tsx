import { useTranslation } from 'react-i18next'
import { UserCircle } from 'lucide-react'
import { Card, CardContent } from '@uikit'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import type { CurrentUser } from '@/types'

interface WelcomeCardProps {
  user: CurrentUser | null
}

export const WelcomeCard = ({ user }: WelcomeCardProps) => {
  const { t, i18n } = useTranslation('common')
  const { timezone } = useRegionalSettings()
  const uiLocale = i18n.language === 'vi' ? 'vi-VN' : 'en-US'

  const now = new Date()
  const formattedDate = now.toLocaleDateString(uiLocale, {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    timeZone: timezone,
  })

  return (
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300 md:col-span-2 xl:col-span-3">
      <CardContent>
        <div className="flex items-center gap-4">
          <div className="p-3 rounded-xl bg-primary/10">
            <UserCircle className="h-8 w-8 text-primary" />
          </div>
          <div className="space-y-0.5">
            <h2 className="text-lg font-semibold leading-snug">
              {t('dashboard.welcomeBack')} {user?.fullName || t('labels.user', 'User')}
            </h2>
            <p className="text-sm text-muted-foreground">
              {formattedDate}
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
