import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Settings, Palette, Phone, Globe, Save } from 'lucide-react'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { usePageContext } from '@/hooks/usePageContext'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Switch } from '@/components/ui/switch'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { ApiError } from '@/services/apiClient'
import {
  getBrandingSettings,
  updateBrandingSettings,
  getContactSettings,
  updateContactSettings,
  getRegionalSettings,
  updateRegionalSettings,
  type BrandingSettingsDto,
  type ContactSettingsDto,
  type RegionalSettingsDto,
} from '@/services/tenantSettings'

/**
 * Tenant Settings Page
 * Tabbed layout for managing branding, contact, and regional settings.
 */
export default function TenantSettingsPage() {
  usePageContext('Tenant Settings')
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.TenantSettingsUpdate)

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center gap-3">
        <div className="p-2 bg-primary/10 rounded-lg">
          <Settings className="h-6 w-6 text-primary" />
        </div>
        <div>
          <h1 className="text-2xl font-bold tracking-tight">
            {t('tenantSettings.title')}
          </h1>
          <p className="text-sm text-muted-foreground">
            {t('tenantSettings.description')}
          </p>
        </div>
      </div>

      {/* Tabbed Content */}
      <Tabs defaultValue="branding" className="space-y-4">
        <TabsList>
          <TabsTrigger value="branding" className="cursor-pointer">
            <Palette className="h-4 w-4 mr-2" />
            {t('tenantSettings.branding.title')}
          </TabsTrigger>
          <TabsTrigger value="contact" className="cursor-pointer">
            <Phone className="h-4 w-4 mr-2" />
            {t('tenantSettings.contact.title')}
          </TabsTrigger>
          <TabsTrigger value="regional" className="cursor-pointer">
            <Globe className="h-4 w-4 mr-2" />
            {t('tenantSettings.regional.title')}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="branding">
          <BrandingSettingsForm canEdit={canEdit} />
        </TabsContent>
        <TabsContent value="contact">
          <ContactSettingsForm canEdit={canEdit} />
        </TabsContent>
        <TabsContent value="regional">
          <RegionalSettingsForm canEdit={canEdit} />
        </TabsContent>
      </Tabs>
    </div>
  )
}

// ============================================================================
// Branding Settings Form
// ============================================================================

function BrandingSettingsForm({ canEdit }: { canEdit: boolean }) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [data, setData] = useState<BrandingSettingsDto | null>(null)

  // Form state
  const [logoUrl, setLogoUrl] = useState('')
  const [faviconUrl, setFaviconUrl] = useState('')
  const [primaryColor, setPrimaryColor] = useState('')
  const [secondaryColor, setSecondaryColor] = useState('')
  const [darkModeDefault, setDarkModeDefault] = useState(false)

  useEffect(() => {
    const loadSettings = async () => {
      try {
        const settings = await getBrandingSettings()
        setData(settings)
        setLogoUrl(settings.logoUrl || '')
        setFaviconUrl(settings.faviconUrl || '')
        setPrimaryColor(settings.primaryColor || '')
        setSecondaryColor(settings.secondaryColor || '')
        setDarkModeDefault(settings.darkModeDefault)
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
      const updated = await updateBrandingSettings({
        logoUrl: logoUrl.trim() || null,
        faviconUrl: faviconUrl.trim() || null,
        primaryColor: primaryColor.trim() || null,
        secondaryColor: secondaryColor.trim() || null,
        darkModeDefault,
      })
      setData(updated)
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
    (logoUrl.trim() || '') !== (data.logoUrl || '') ||
    (faviconUrl.trim() || '') !== (data.faviconUrl || '') ||
    (primaryColor.trim() || '') !== (data.primaryColor || '') ||
    (secondaryColor.trim() || '') !== (data.secondaryColor || '') ||
    darkModeDefault !== data.darkModeDefault
  )

  if (loading) {
    return (
      <Card>
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
    <Card>
      <CardHeader>
        <CardTitle>{t('tenantSettings.branding.title')}</CardTitle>
        <CardDescription>{t('tenantSettings.branding.description')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="logoUrl">{t('tenantSettings.branding.logoUrl')}</Label>
            <Input
              id="logoUrl"
              value={logoUrl}
              onChange={(e) => setLogoUrl(e.target.value)}
              placeholder="https://example.com/logo.png"
              disabled={!canEdit}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="faviconUrl">{t('tenantSettings.branding.faviconUrl')}</Label>
            <Input
              id="faviconUrl"
              value={faviconUrl}
              onChange={(e) => setFaviconUrl(e.target.value)}
              placeholder="https://example.com/favicon.ico"
              disabled={!canEdit}
            />
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="primaryColor">{t('tenantSettings.branding.primaryColor')}</Label>
            <div className="flex gap-2">
              <Input
                id="primaryColor"
                value={primaryColor}
                onChange={(e) => setPrimaryColor(e.target.value)}
                placeholder="#3B82F6"
                disabled={!canEdit}
                className="flex-1"
              />
              {primaryColor && (
                <div
                  className="w-10 h-10 rounded border"
                  style={{ backgroundColor: primaryColor }}
                />
              )}
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="secondaryColor">{t('tenantSettings.branding.secondaryColor')}</Label>
            <div className="flex gap-2">
              <Input
                id="secondaryColor"
                value={secondaryColor}
                onChange={(e) => setSecondaryColor(e.target.value)}
                placeholder="#6366F1"
                disabled={!canEdit}
                className="flex-1"
              />
              {secondaryColor && (
                <div
                  className="w-10 h-10 rounded border"
                  style={{ backgroundColor: secondaryColor }}
                />
              )}
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between rounded-lg border p-4">
          <div className="space-y-0.5">
            <Label>{t('tenantSettings.branding.darkModeDefault')}</Label>
            <p className="text-sm text-muted-foreground">
              {t('tenantSettings.branding.darkModeDescription')}
            </p>
          </div>
          <Switch
            checked={darkModeDefault}
            onCheckedChange={setDarkModeDefault}
            disabled={!canEdit}
            className="cursor-pointer"
          />
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

// ============================================================================
// Contact Settings Form
// ============================================================================

function ContactSettingsForm({ canEdit }: { canEdit: boolean }) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [data, setData] = useState<ContactSettingsDto | null>(null)

  // Form state
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [address, setAddress] = useState('')

  useEffect(() => {
    const loadSettings = async () => {
      try {
        const settings = await getContactSettings()
        setData(settings)
        setEmail(settings.email || '')
        setPhone(settings.phone || '')
        setAddress(settings.address || '')
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
      const updated = await updateContactSettings({
        email: email.trim() || null,
        phone: phone.trim() || null,
        address: address.trim() || null,
      })
      setData(updated)
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
    (email.trim() || '') !== (data.email || '') ||
    (phone.trim() || '') !== (data.phone || '') ||
    (address.trim() || '') !== (data.address || '')
  )

  if (loading) {
    return (
      <Card>
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
    <Card>
      <CardHeader>
        <CardTitle>{t('tenantSettings.contact.title')}</CardTitle>
        <CardDescription>{t('tenantSettings.contact.description')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="contactEmail">{t('tenantSettings.contact.email')}</Label>
          <Input
            id="contactEmail"
            type="text"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="contact@example.com"
            disabled={!canEdit}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="contactPhone">{t('tenantSettings.contact.phone')}</Label>
          <Input
            id="contactPhone"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            placeholder="+1 (555) 123-4567"
            disabled={!canEdit}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="contactAddress">{t('tenantSettings.contact.address')}</Label>
          <Textarea
            id="contactAddress"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            placeholder="123 Business St, Suite 100, City, State 12345"
            className="min-h-[80px]"
            disabled={!canEdit}
          />
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

// ============================================================================
// Regional Settings Form
// ============================================================================

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

const LANGUAGE_OPTIONS = [
  { value: 'en', label: 'English' },
  { value: 'vi', label: 'Tiếng Việt' },
  { value: 'ja', label: '日本語' },
  { value: 'ko', label: '한국어' },
  { value: 'zh', label: '中文' },
  { value: 'fr', label: 'Français' },
  { value: 'de', label: 'Deutsch' },
  { value: 'es', label: 'Español' },
  { value: 'it', label: 'Italiano' },
  { value: 'pt', label: 'Português' },
]

const DATE_FORMAT_OPTIONS = [
  { value: 'YYYY-MM-DD', label: 'YYYY-MM-DD (ISO)' },
  { value: 'MM/DD/YYYY', label: 'MM/DD/YYYY (US)' },
  { value: 'DD/MM/YYYY', label: 'DD/MM/YYYY (EU)' },
  { value: 'DD.MM.YYYY', label: 'DD.MM.YYYY (DE)' },
]

function RegionalSettingsForm({ canEdit }: { canEdit: boolean }) {
  const { t } = useTranslation('common')
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
      <Card>
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
    <Card>
      <CardHeader>
        <CardTitle>{t('tenantSettings.regional.title')}</CardTitle>
        <CardDescription>{t('tenantSettings.regional.description')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="timezone">{t('tenantSettings.regional.timezone')}</Label>
          <Select value={timezone} onValueChange={setTimezone} disabled={!canEdit}>
            <SelectTrigger className="cursor-pointer">
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
          <Select value={language} onValueChange={setLanguage} disabled={!canEdit}>
            <SelectTrigger className="cursor-pointer">
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
          <Select value={dateFormat} onValueChange={setDateFormat} disabled={!canEdit}>
            <SelectTrigger className="cursor-pointer">
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
