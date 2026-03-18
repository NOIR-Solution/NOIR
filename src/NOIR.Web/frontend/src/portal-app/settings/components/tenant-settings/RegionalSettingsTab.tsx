import { useState, useEffect, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Save } from 'lucide-react'
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Combobox,
  Label,
  Skeleton,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'

import { ApiError } from '@/services/apiClient'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import {
  useRegionalSettingsQuery,
  useUpdateRegionalSettings,
} from '@/portal-app/settings/queries'
import { getTimezoneOptions } from '@/lib/timezones'

// Only languages with actual translation files
const LANGUAGE_OPTIONS = [
  { value: 'en', label: 'English' },
  { value: 'vi', label: 'Tiếng Việt' },
]

const DATE_FORMAT_OPTIONS = [
  { value: 'YYYY-MM-DD', label: 'YYYY-MM-DD (ISO)' },
  { value: 'MM/DD/YYYY', label: 'MM/DD/YYYY (US)' },
  { value: 'DD/MM/YYYY', label: 'DD/MM/YYYY (EU)' },
  { value: 'DD.MM.YYYY', label: 'DD.MM.YYYY (DE)' },
]

export interface RegionalSettingsTabProps {
  canEdit: boolean
}

export const RegionalSettingsTab = ({ canEdit }: RegionalSettingsTabProps) => {
  const { t } = useTranslation('common')
  const { reloadRegional } = useRegionalSettings()
  const { data, isLoading } = useRegionalSettingsQuery()
  const updateMutation = useUpdateRegionalSettings()
  const timezoneOptions = useMemo(() => getTimezoneOptions(), [])

  // Form state
  const [timezone, setTimezone] = useState('Asia/Ho_Chi_Minh')
  const [language, setLanguage] = useState('vi')
  const [dateFormat, setDateFormat] = useState('DD/MM/YYYY')

  useEffect(() => {
    if (data) {
      setTimezone(data.timezone)
      setLanguage(data.language)
      setDateFormat(data.dateFormat)
    }
  }, [data])

  const handleSave = () => {
    updateMutation.mutate(
      {
        timezone,
        language,
        dateFormat,
      },
      {
        onSuccess: async () => {
          await reloadRegional()
          toast.success(t('tenantSettings.saved'))
        },
        onError: (error) => {
          if (error instanceof ApiError) {
            toast.error(error.message)
          } else {
            toast.error(t('messages.operationFailed'))
          }
        },
      },
    )
  }

  const hasChanges = data && (
    timezone !== data.timezone ||
    language !== data.language ||
    dateFormat !== data.dateFormat
  )

  if (isLoading) {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardContent className="py-8">
          <div className="space-y-4">
            <Skeleton className="h-4 w-48" />
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-4 w-48" />
            <Skeleton className="h-10 w-full" />
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
      <CardHeader>
        <CardTitle className="text-lg">{t('tenantSettings.regional.title')}</CardTitle>
        <CardDescription>{t('tenantSettings.regional.description')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="timezone">{t('tenantSettings.regional.timezone')}</Label>
          <p className="text-sm text-muted-foreground">
            {t('tenantSettings.regional.timezoneDescription')}
          </p>
          <Combobox
            options={timezoneOptions}
            value={timezone}
            onValueChange={setTimezone}
            placeholder={t('tenantSettings.regional.selectTimezone', 'Select timezone...')}
            searchPlaceholder={t('tenantSettings.regional.searchTimezones', 'Search timezones...')}
            countLabel={t('tenantSettings.regional.timezones', 'timezones')}
            disabled={!canEdit}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="language">{t('tenantSettings.regional.language')}</Label>
          <p className="text-sm text-muted-foreground">
            {t('tenantSettings.regional.languageDescription')}
          </p>
          <Select value={language} onValueChange={setLanguage} disabled={!canEdit}>
            <SelectTrigger className="cursor-pointer" aria-label={t('tenantSettings.regional.language', 'Language')}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {LANGUAGE_OPTIONS.map((opt) => (
                <SelectItem key={opt.value} value={opt.value} className="cursor-pointer">
                  {opt.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="dateFormat">{t('tenantSettings.regional.dateFormat')}</Label>
          <p className="text-sm text-muted-foreground">
            {t('tenantSettings.regional.dateFormatDescription')}
          </p>
          <Select value={dateFormat} onValueChange={setDateFormat} disabled={!canEdit}>
            <SelectTrigger className="cursor-pointer" aria-label={t('tenantSettings.regional.dateFormat', 'Date format')}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {DATE_FORMAT_OPTIONS.map((opt) => (
                <SelectItem key={opt.value} value={opt.value} className="cursor-pointer">
                  {opt.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {canEdit && (
          <div className="flex justify-end">
            <Button onClick={handleSave} disabled={updateMutation.isPending || !hasChanges}>
              <Save className="h-4 w-4 mr-2" />
              {updateMutation.isPending ? t('buttons.saving') : t('buttons.save')}
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
