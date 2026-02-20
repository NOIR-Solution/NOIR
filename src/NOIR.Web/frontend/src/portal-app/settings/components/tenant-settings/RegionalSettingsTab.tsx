import { useState, useEffect } from 'react'
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
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'

import { ApiError } from '@/services/apiClient'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import {
  getRegionalSettings,
  updateRegionalSettings,
  type RegionalSettingsDto,
} from '@/services/tenantSettings'

const TIMEZONE_OPTIONS = [
  { value: 'UTC', label: 'UTC' },
  { value: 'America/New_York', label: 'Eastern Time (US)' },
  { value: 'America/Chicago', label: 'Central Time (US)' },
  { value: 'America/Denver', label: 'Mountain Time (US)' },
  { value: 'America/Los_Angeles', label: 'Pacific Time (US)' },
  { value: 'Europe/London', label: 'London (GMT)' },
  { value: 'Europe/Paris', label: 'Paris (CET)' },
  { value: 'Europe/Berlin', label: 'Berlin (CET)' },
  { value: 'Asia/Tokyo', label: 'Tokyo (JST)' },
  { value: 'Asia/Shanghai', label: 'Shanghai (CST)' },
  { value: 'Asia/Seoul', label: 'Seoul (KST)' },
  { value: 'Asia/Ho_Chi_Minh', label: 'Ho Chi Minh (ICT)' },
  { value: 'Australia/Sydney', label: 'Sydney (AEST)' },
]

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
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [data, setData] = useState<RegionalSettingsDto | null>(null)

  // Form state
  const [timezone, setTimezone] = useState('UTC')
  const [language, setLanguage] = useState('en')
  const [dateFormat, setDateFormat] = useState('YYYY-MM-DD')

  useEffect(() => {
    const loadSettings = async () => {
      try {
        const settings = await getRegionalSettings()
        setData(settings)
        setTimezone(settings.timezone)
        setLanguage(settings.language)
        setDateFormat(settings.dateFormat)
      } catch (error) {
        if (error instanceof ApiError) {
          toast.error(error.message)
        }
      } finally {
        setLoading(false)
      }
    }
    loadSettings()
  }, [])

  const handleSave = async () => {
    setSaving(true)
    try {
      const updated = await updateRegionalSettings({
        timezone,
        language,
        dateFormat,
      })
      setData(updated)
      // Reload regional context to apply changes globally (timezone, date format, language default)
      await reloadRegional()
      toast.success(t('tenantSettings.saved'))
    } catch (error) {
      if (error instanceof ApiError) {
        toast.error(error.message)
      } else {
        toast.error(t('messages.operationFailed'))
      }
    } finally {
      setSaving(false)
    }
  }

  const hasChanges = data && (
    timezone !== data.timezone ||
    language !== data.language ||
    dateFormat !== data.dateFormat
  )

  if (loading) {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardContent className="py-8">
          <div className="animate-pulse space-y-4">
            <div className="h-4 w-48 bg-muted rounded" />
            <div className="h-10 w-full bg-muted rounded" />
            <div className="h-4 w-48 bg-muted rounded" />
            <div className="h-10 w-full bg-muted rounded" />
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
          <Select value={timezone} onValueChange={setTimezone} disabled={!canEdit}>
            <SelectTrigger className="cursor-pointer" aria-label={t('tenantSettings.regional.timezone', 'Timezone')}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {TIMEZONE_OPTIONS.map((opt) => (
                <SelectItem key={opt.value} value={opt.value} className="cursor-pointer">
                  {opt.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
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
            <Button onClick={handleSave} disabled={saving || !hasChanges}>
              <Save className="h-4 w-4 mr-2" />
              {saving ? t('buttons.saving') : t('buttons.save')}
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
