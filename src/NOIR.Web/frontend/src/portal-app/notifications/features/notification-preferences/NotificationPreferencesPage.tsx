/**
 * NotificationPreferences Page
 *
 * Manage notification preferences per category.
 */
import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { usePageContext } from '@/hooks/usePageContext'
import { ArrowLeft, Save, Bell, Mail, Shield, Workflow, Users, Settings2, Loader2 } from 'lucide-react'
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Label,
  PageHeader,
  Skeleton,
  Switch,
} from '@uikit'

import { toast } from 'sonner'
import { getPreferences, updatePreferences } from '@/services/notifications'
import type { NotificationPreference, NotificationCategory, EmailFrequency } from '@/types'
import { cn } from '@/lib/utils'

const categoryConfig: Record<string, { icon: typeof Bell }> = {
  system: { icon: Settings2 },
  userAction: { icon: Users },
  workflow: { icon: Workflow },
  security: { icon: Shield },
  integration: { icon: Bell },
}

const emailFrequencyOptions: { value: EmailFrequency }[] = [
  { value: 'none' },
  { value: 'immediate' },
  { value: 'daily' },
  { value: 'weekly' },
]

/** Normalize PascalCase API enum to camelCase locale key (e.g. "UserAction" → "userAction") */
const toCamelCase = (s: string) => s.charAt(0).toLowerCase() + s.slice(1)

export const NotificationPreferencesPage = () => {
  const { t } = useTranslation('common')
  usePageContext('NotificationPreferences')
  const [preferences, setPreferences] = useState<NotificationPreference[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [hasChanges, setHasChanges] = useState(false)

  // Fetch preferences on mount
  useEffect(() => {
    const fetchPreferences = async () => {
      try {
        const data = await getPreferences()
        setPreferences(data)
      } catch {
        toast.error(t('notifications.failedToLoad'))
      } finally {
        setIsLoading(false)
      }
    }
    fetchPreferences()
  }, [])

  const handleInAppToggle = (category: NotificationCategory) => {
    setPreferences((prev) =>
      prev.map((p) =>
        p.category === category ? { ...p, inAppEnabled: !p.inAppEnabled } : p
      )
    )
    setHasChanges(true)
  }

  const handleEmailFrequencyChange = (category: NotificationCategory, frequency: EmailFrequency) => {
    setPreferences((prev) =>
      prev.map((p) =>
        p.category === category ? { ...p, emailFrequency: frequency } : p
      )
    )
    setHasChanges(true)
  }

  const handleSave = async () => {
    setIsSaving(true)
    try {
      await updatePreferences({
        preferences: preferences.map((p) => ({
          category: p.category,
          inAppEnabled: p.inAppEnabled,
          emailFrequency: p.emailFrequency,
        })),
      })
      toast.success(t('notifications.savedSuccessfully'))
      setHasChanges(false)
    } catch {
      toast.error(t('notifications.failedToSave'))
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) {
    return (
      <div className="container max-w-4xl py-6">
        {/* Header skeleton */}
        <div className="flex items-center justify-between mb-8">
          <div className="space-y-1">
            <div className="flex items-center gap-3">
              <Skeleton className="h-8 w-8 rounded" />
              <Skeleton className="h-7 w-[220px]" />
            </div>
            <Skeleton className="h-4 w-[320px] ml-11" />
          </div>
          <Skeleton className="h-10 w-[130px]" />
        </div>
        {/* Cards skeleton - matches actual content structure */}
        <div className="space-y-4">
          {Array.from({ length: 5 }).map((_, i) => (
            <Card key={i} className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardHeader className="pb-4">
                <div className="flex items-center gap-3">
                  <Skeleton className="h-10 w-10 rounded-lg" />
                  <div className="space-y-2">
                    <Skeleton className="h-4 w-[100px]" />
                    <Skeleton className="h-3 w-[250px]" />
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-4 pt-0">
                <div className="flex items-center justify-between py-2">
                  <div className="flex items-center gap-3">
                    <Skeleton className="h-4 w-4 rounded" />
                    <Skeleton className="h-4 w-[140px]" />
                  </div>
                  <Skeleton className="h-6 w-11 rounded-full" />
                </div>
                <div className="space-y-3">
                  <div className="flex items-center gap-3">
                    <Skeleton className="h-4 w-4 rounded" />
                    <Skeleton className="h-4 w-[130px]" />
                  </div>
                  <div className="flex gap-2 ml-7">
                    {Array.from({ length: 4 }).map((_, j) => (
                      <Skeleton key={j} className="h-8 w-[80px] rounded-md" />
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className="container max-w-4xl py-6">
      {/* Page Header */}
      <div className="flex items-center justify-between mb-8">
        <div className="flex items-center gap-4">
          <Button
            variant="ghost"
            size="icon"
            asChild
            className="cursor-pointer"
            aria-label={t('common.back', 'Back')}
          >
            <ViewTransitionLink to="/portal/notifications">
              <ArrowLeft className="h-5 w-5" />
            </ViewTransitionLink>
          </Button>
          <PageHeader
            icon={Bell}
            title={t('notifications.preferencesTitle')}
            description={t('notifications.preferencesDescription')}
            responsive
          />
        </div>
        <Button onClick={handleSave} disabled={!hasChanges || isSaving} className="cursor-pointer">
          {isSaving ? (
            <Loader2 className="h-4 w-4 mr-2 animate-spin" />
          ) : (
            <Save className="h-4 w-4 mr-2" />
          )}
          {isSaving ? t('buttons.saving') : t('notifications.saveChanges')}
        </Button>
      </div>

      {/* Preferences Grid */}
      <div className="space-y-4">
        {preferences.map((pref) => {
          const categoryKey = toCamelCase(pref.category)
          const config = categoryConfig[categoryKey] || categoryConfig.system
          const Icon = config.icon

          return (
            <Card key={pref.category} className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardHeader className="pb-4">
                <div className="flex items-center gap-3">
                  <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-primary/10">
                    <Icon className="h-5 w-5 text-primary" />
                  </div>
                  <div>
                    <CardTitle className="text-base">{t(`notifications.categories.${categoryKey}`)}</CardTitle>
                    <CardDescription className="text-sm">{t(`notifications.categories.${categoryKey}Description`)}</CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-4 pt-0">
                {/* In-app notifications toggle */}
                <div className="flex items-center justify-between py-2">
                  <div className="flex items-center gap-3">
                    <Bell className="h-4 w-4 text-muted-foreground" />
                    <Label htmlFor={`inapp-${pref.category}`} className="cursor-pointer font-normal">
                      {t('notifications.inAppNotifications')}
                    </Label>
                  </div>
                  <Switch
                    id={`inapp-${pref.category}`}
                    checked={pref.inAppEnabled}
                    onCheckedChange={() => handleInAppToggle(pref.category)}
                    className="cursor-pointer"
                    aria-label={t('notifications.inAppNotifications')}
                  />
                </div>

                {/* Email frequency */}
                <div className="space-y-3">
                  <div className="flex items-center gap-3">
                    <Mail className="h-4 w-4 text-muted-foreground" />
                    <Label className="font-normal">{t('notifications.emailNotifications')}</Label>
                  </div>
                  <div className="flex flex-wrap gap-2 ml-7">
                    {emailFrequencyOptions.map((option) => (
                      <button
                        key={option.value}
                        onClick={() => handleEmailFrequencyChange(pref.category, option.value)}
                        className={cn(
                          'px-3 py-1.5 text-sm rounded-md border transition-colors cursor-pointer',
                          pref.emailFrequency === option.value
                            ? 'bg-primary text-primary-foreground border-primary'
                            : 'bg-background hover:bg-muted border-input'
                        )}
                      >
                        {t(`notifications.emailFrequency.${option.value}`)}
                      </button>
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>
          )
        })}
      </div>

      {/* Info */}
      <p className="text-sm text-muted-foreground mt-6">
        {t('notifications.securityNote')}
      </p>
    </div>
  )
}

export default NotificationPreferencesPage
