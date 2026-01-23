import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import {
  Settings,
  Palette,
  Phone,
  Globe,
  Save,
  Mail,
  FileText,
  Scale,
  Send,
  Loader2,
  Check,
  AlertCircle,
  Server,
  Pencil,
  Eye,
  GitFork,
  Info,
  RotateCcw,
} from 'lucide-react'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { usePageContext } from '@/hooks/usePageContext'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Switch } from '@/components/ui/switch'
import { Badge } from '@/components/ui/badge'
import { ColorPicker } from '@/components/ui/color-picker'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
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
  getTenantSmtpSettings,
  updateTenantSmtpSettings,
  revertTenantSmtpSettings,
  testTenantSmtpConnection,
  type BrandingSettingsDto,
  type ContactSettingsDto,
  type RegionalSettingsDto,
  type TenantSmtpSettingsDto,
} from '@/services/tenantSettings'
import { getEmailTemplates, type EmailTemplateListDto } from '@/services/emailTemplates'
import { getLegalPages, type LegalPageListDto } from '@/services/legalPages'

// ============================================================================
// Tenant SMTP Form Schema
// ============================================================================
const tenantSmtpSettingsSchema = z.object({
  host: z.string().min(1, 'SMTP host is required'),
  port: z.coerce.number().int().min(1).max(65535),
  username: z.string().optional().nullable(),
  password: z.string().optional().nullable(),
  fromEmail: z.string().email('Invalid email address'),
  fromName: z.string().min(1, 'From name is required'),
  useSsl: z.boolean(),
})

type TenantSmtpFormData = z.infer<typeof tenantSmtpSettingsSchema>

const testEmailSchema = z.object({
  recipientEmail: z.string().email('Invalid email address'),
})

type TestEmailFormData = z.infer<typeof testEmailSchema>

/**
 * Tenant Settings Page
 * Tabbed layout for managing branding, contact, regional, SMTP, email templates, and legal pages.
 */
export default function TenantSettingsPage() {
  usePageContext('Tenant Settings')
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.TenantSettingsUpdate)
  const [searchParams, setSearchParams] = useSearchParams()

  const [activeTab, setActiveTab] = useState(() => searchParams.get('tab') || 'branding')

  const handleTabChange = (tab: string) => {
    setActiveTab(tab)
    setSearchParams({ tab }, { replace: true })
  }

  return (
    <div className="container max-w-4xl py-6 space-y-6">
      {/* Page Header */}
      <div className="flex items-center gap-3">
        <div className="h-10 w-10 rounded-lg bg-primary/10 flex items-center justify-center">
          <Settings className="h-5 w-5 text-primary" />
        </div>
        <div>
          <h1 className="text-2xl font-bold tracking-tight">
            {t('tenantSettings.title')}
          </h1>
          <p className="text-muted-foreground">
            {t('tenantSettings.description')}
          </p>
        </div>
      </div>

      {/* Tabbed Content */}
      <Tabs value={activeTab} onValueChange={handleTabChange} className="space-y-4">
        <TabsList className="flex-wrap h-auto">
          <TabsTrigger value="branding" className="cursor-pointer">
            <Palette className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.branding')}
          </TabsTrigger>
          <TabsTrigger value="contact" className="cursor-pointer">
            <Phone className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.contact')}
          </TabsTrigger>
          <TabsTrigger value="regional" className="cursor-pointer">
            <Globe className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.regional')}
          </TabsTrigger>
          <TabsTrigger value="smtp" className="cursor-pointer">
            <Mail className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.smtp')}
          </TabsTrigger>
          <TabsTrigger value="emailTemplates" className="cursor-pointer">
            <FileText className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.emailTemplates')}
          </TabsTrigger>
          <TabsTrigger value="legalPages" className="cursor-pointer">
            <Scale className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.legalPages')}
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
        <TabsContent value="smtp">
          <TenantSmtpSettingsTab canEdit={canEdit} />
        </TabsContent>
        <TabsContent value="emailTemplates">
          <TenantEmailTemplatesTab
            onEdit={(id) => navigate(`/portal/email-templates/${id}`)}
            onView={(id) => navigate(`/portal/email-templates/${id}?mode=preview`)}
          />
        </TabsContent>
        <TabsContent value="legalPages">
          <TenantLegalPagesTab onEdit={(id) => navigate(`/portal/legal-pages/${id}`)} />
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

        <div className="grid gap-6 sm:grid-cols-2">
          <div className="space-y-2">
            <Label>{t('tenantSettings.branding.primaryColor')}</Label>
            <ColorPicker
              value={primaryColor || '#3B82F6'}
              onChange={setPrimaryColor}
              showCustomInput={canEdit}
            />
          </div>
          <div className="space-y-2">
            <Label>{t('tenantSettings.branding.secondaryColor')}</Label>
            <ColorPicker
              value={secondaryColor || '#6366F1'}
              onChange={setSecondaryColor}
              showCustomInput={canEdit}
            />
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

// ============================================================================
// Tenant SMTP Settings Tab
// ============================================================================
function TenantSmtpSettingsTab({ canEdit }: { canEdit: boolean }) {
  const { t } = useTranslation('common')

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [testing, setTesting] = useState(false)
  const [testDialogOpen, setTestDialogOpen] = useState(false)
  const [isConfigured, setIsConfigured] = useState(false)
  const [isInherited, setIsInherited] = useState(true)
  const [hasPassword, setHasPassword] = useState(false)
  const [reverting, setReverting] = useState(false)

  const form = useForm<TenantSmtpFormData>({
    resolver: zodResolver(tenantSmtpSettingsSchema),
    defaultValues: {
      host: '',
      port: 587,
      username: '',
      password: '',
      fromEmail: '',
      fromName: '',
      useSsl: true,
    },
    mode: 'onBlur',
  })

  const testForm = useForm<TestEmailFormData>({
    resolver: zodResolver(testEmailSchema),
    defaultValues: {
      recipientEmail: '',
    },
  })

  useEffect(() => {
    const loadSettings = async () => {
      try {
        const settings = await getTenantSmtpSettings()
        setIsConfigured(settings.isConfigured)
        setIsInherited(settings.isInherited)
        setHasPassword(settings.hasPassword)

        form.reset({
          host: settings.host,
          port: settings.port,
          username: settings.username ?? '',
          password: '',
          fromEmail: settings.fromEmail,
          fromName: settings.fromName,
          useSsl: settings.useSsl,
        })
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Failed to load settings'
        toast.error(message)
      } finally {
        setLoading(false)
      }
    }

    loadSettings()
  }, [form])

  const onSubmit = async (data: TenantSmtpFormData) => {
    setSaving(true)
    try {
      const result = await updateTenantSmtpSettings({
        host: data.host,
        port: data.port,
        username: data.username || null,
        password: data.password || null,
        fromEmail: data.fromEmail,
        fromName: data.fromName,
        useSsl: data.useSsl,
      })

      setIsConfigured(result.isConfigured)
      setIsInherited(result.isInherited)
      setHasPassword(result.hasPassword)
      form.setValue('password', '')

      toast.success(t('tenantSettings.saved'))
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to save settings'
      toast.error(message)
    } finally {
      setSaving(false)
    }
  }

  const handleRevert = async () => {
    setReverting(true)
    try {
      const result = await revertTenantSmtpSettings()
      setIsConfigured(result.isConfigured)
      setIsInherited(result.isInherited)
      setHasPassword(result.hasPassword)

      form.reset({
        host: result.host,
        port: result.port,
        username: result.username ?? '',
        password: '',
        fromEmail: result.fromEmail,
        fromName: result.fromName,
        useSsl: result.useSsl,
      })

      toast.success(t('tenantSettings.saved'))
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to revert settings'
      toast.error(message)
    } finally {
      setReverting(false)
    }
  }

  const onTestSubmit = async (data: TestEmailFormData) => {
    setTesting(true)
    try {
      await testTenantSmtpConnection({ recipientEmail: data.recipientEmail })
      toast.success(t('platformSettings.smtp.testSuccess'))
      setTestDialogOpen(false)
      testForm.reset()
    } catch (err) {
      const message = err instanceof ApiError ? err.message : t('platformSettings.smtp.testFailed')
      toast.error(message)
    } finally {
      setTesting(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <>
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="h-8 w-8 rounded-md bg-blue-500/10 flex items-center justify-center">
                <Mail className="h-4 w-4 text-blue-500" />
              </div>
              <div>
                <CardTitle className="text-lg">{t('platformSettings.smtp.title')}</CardTitle>
                <CardDescription>{t('platformSettings.smtp.description')}</CardDescription>
              </div>
            </div>
            <div className="flex items-center gap-2">
              {!isInherited && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleRevert}
                  disabled={reverting}
                >
                  <RotateCcw className="h-3 w-3 mr-1" />
                  {t('legalPages.revertToDefault')}
                </Button>
              )}
              <Badge variant={isInherited ? 'outline' : 'default'}>
                {isInherited ? (
                  <>
                    <GitFork className="h-3 w-3 mr-1" />
                    {t('legalPages.platformDefault')}
                  </>
                ) : (
                  <>
                    <Check className="h-3 w-3 mr-1" />
                    {t('legalPages.customized')}
                  </>
                )}
              </Badge>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {/* Copy-on-Write notice */}
          {isInherited && (
            <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-lg p-3 text-sm text-purple-800 dark:text-purple-200 flex items-start gap-3 mb-6">
              <Info className="h-5 w-5 flex-shrink-0 mt-0.5" />
              <div>
                <p className="font-medium">{t('legalPages.customizingPlatform')}</p>
                <p className="text-purple-600 dark:text-purple-300 mt-1">
                  {t('legalPages.customizingPlatformDescription')}
                </p>
              </div>
            </div>
          )}

          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
              <div className="grid gap-4 md:grid-cols-2">
                <FormField
                  control={form.control}
                  name="host"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('platformSettings.smtp.host')}</FormLabel>
                      <FormControl>
                        <Input
                          placeholder={t('platformSettings.smtp.hostPlaceholder')}
                          {...field}
                          disabled={!canEdit}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="port"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('platformSettings.smtp.port')}</FormLabel>
                      <FormControl>
                        <Input type="number" placeholder="587" {...field} disabled={!canEdit} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <FormField
                  control={form.control}
                  name="username"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('platformSettings.smtp.username')}</FormLabel>
                      <FormControl>
                        <Input
                          placeholder={t('platformSettings.smtp.usernamePlaceholder')}
                          {...field}
                          value={field.value ?? ''}
                          disabled={!canEdit}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="password"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('platformSettings.smtp.password')}</FormLabel>
                      <FormControl>
                        <Input
                          type="password"
                          placeholder={
                            hasPassword
                              ? t('platformSettings.smtp.passwordPlaceholder')
                              : 'Enter password'
                          }
                          {...field}
                          value={field.value ?? ''}
                          disabled={!canEdit}
                        />
                      </FormControl>
                      {hasPassword && !field.value && (
                        <FormDescription className="text-xs text-amber-600">
                          {t('platformSettings.smtp.passwordHidden')}
                        </FormDescription>
                      )}
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <FormField
                  control={form.control}
                  name="fromEmail"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('platformSettings.smtp.fromEmail')}</FormLabel>
                      <FormControl>
                        <Input
                          type="email"
                          placeholder={t('platformSettings.smtp.fromEmailPlaceholder')}
                          {...field}
                          disabled={!canEdit}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="fromName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('platformSettings.smtp.fromName')}</FormLabel>
                      <FormControl>
                        <Input
                          placeholder={t('platformSettings.smtp.fromNamePlaceholder')}
                          {...field}
                          disabled={!canEdit}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="useSsl"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
                    <div className="space-y-0.5">
                      <FormLabel className="text-base">{t('platformSettings.smtp.useSsl')}</FormLabel>
                      <FormDescription>{t('platformSettings.smtp.useSslHint')}</FormDescription>
                    </div>
                    <FormControl>
                      <Switch checked={field.value} onCheckedChange={field.onChange} disabled={!canEdit} />
                    </FormControl>
                  </FormItem>
                )}
              />

              {canEdit && (
                <div className="flex items-center justify-between pt-4 border-t">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => setTestDialogOpen(true)}
                    disabled={!isConfigured || saving}
                  >
                    <Send className="h-4 w-4 mr-2" />
                    {t('platformSettings.smtp.testConnection')}
                  </Button>
                  <Button type="submit" disabled={saving}>
                    {saving ? (
                      <>
                        <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                        {t('buttons.saving')}
                      </>
                    ) : (
                      t('buttons.save')
                    )}
                  </Button>
                </div>
              )}
            </form>
          </Form>
        </CardContent>
      </Card>

      {/* Test Connection Dialog */}
      <Dialog open={testDialogOpen} onOpenChange={setTestDialogOpen}>
        <DialogContent className="sm:max-w-[425px]">
          <DialogHeader>
            <DialogTitle>{t('platformSettings.smtp.testConnectionTitle')}</DialogTitle>
            <DialogDescription>{t('platformSettings.smtp.testConnectionDescription')}</DialogDescription>
          </DialogHeader>
          <Form {...testForm}>
            <form onSubmit={testForm.handleSubmit(onTestSubmit)} className="space-y-4">
              <FormField
                control={testForm.control}
                name="recipientEmail"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('platformSettings.smtp.testRecipient')}</FormLabel>
                    <FormControl>
                      <Input
                        type="email"
                        placeholder={t('platformSettings.smtp.testRecipientPlaceholder')}
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setTestDialogOpen(false)} disabled={testing}>
                  {t('buttons.cancel')}
                </Button>
                <Button type="submit" disabled={testing}>
                  {testing ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Sending...
                    </>
                  ) : (
                    <>
                      <Send className="h-4 w-4 mr-2" />
                      Send Test
                    </>
                  )}
                </Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </>
  )
}

// ============================================================================
// Tenant Email Templates Tab
// ============================================================================
function TenantEmailTemplatesTab({ onEdit, onView }: { onEdit: (id: string) => void; onView: (id: string) => void }) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(true)
  const [templates, setTemplates] = useState<EmailTemplateListDto[]>([])

  useEffect(() => {
    const loadTemplates = async () => {
      try {
        const data = await getEmailTemplates()
        setTemplates(data)
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Failed to load templates'
        toast.error(message)
      } finally {
        setLoading(false)
      }
    }
    loadTemplates()
  }, [])

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">{t('emailTemplates.title')}</CardTitle>
        <CardDescription>{t('emailTemplates.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 md:grid-cols-2">
          {templates.map((template) => (
            <Card key={template.id} className="overflow-hidden">
              <CardContent className="p-4">
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <h4 className="font-medium">{template.name}</h4>
                    <p className="text-sm text-muted-foreground line-clamp-2">
                      {template.description}
                    </p>
                    <div className="flex items-center gap-2 pt-2">
                      <Badge variant={template.isActive ? 'default' : 'secondary'} className="text-xs">
                        {template.isActive ? t('labels.active') : t('labels.inactive')}
                      </Badge>
                      <Badge
                        variant="outline"
                        className={`text-xs ${
                          template.isInherited
                            ? 'text-purple-600 border-purple-600/30'
                            : 'text-green-600 border-green-600/30'
                        }`}
                      >
                        <GitFork className="h-3 w-3 mr-1" />
                        {template.isInherited ? t('legalPages.platformDefault') : t('legalPages.customized')}
                      </Badge>
                    </div>
                  </div>
                  <div className="flex flex-col gap-1">
                    <Button variant="ghost" size="icon" onClick={() => onEdit(template.id)}>
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button variant="ghost" size="icon" onClick={() => onView(template.id)}>
                      <Eye className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
        {templates.length === 0 && (
          <div className="text-center py-8 text-muted-foreground">
            No email templates found.
          </div>
        )}
      </CardContent>
    </Card>
  )
}

// ============================================================================
// Tenant Legal Pages Tab
// ============================================================================
function TenantLegalPagesTab({ onEdit }: { onEdit: (id: string) => void }) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(true)
  const [pages, setPages] = useState<LegalPageListDto[]>([])

  useEffect(() => {
    const loadPages = async () => {
      try {
        const data = await getLegalPages()
        setPages(data)
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Failed to load legal pages'
        toast.error(message)
      } finally {
        setLoading(false)
      }
    }
    loadPages()
  }, [])

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">{t('legalPages.title')}</CardTitle>
        <CardDescription>{t('legalPages.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 md:grid-cols-2">
          {pages.map((page) => (
            <Card key={page.id} className="overflow-hidden">
              <CardContent className="p-4">
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <h4 className="font-medium">{page.title}</h4>
                    <p className="text-sm text-muted-foreground">/{page.slug}</p>
                    <div className="flex items-center gap-2 pt-2">
                      <Badge variant={page.isActive ? 'default' : 'secondary'} className="text-xs">
                        {page.isActive ? t('labels.active') : t('labels.inactive')}
                      </Badge>
                      <Badge
                        variant="outline"
                        className={`text-xs ${
                          page.isInherited
                            ? 'text-purple-600 border-purple-600/30'
                            : 'text-green-600 border-green-600/30'
                        }`}
                      >
                        <GitFork className="h-3 w-3 mr-1" />
                        {page.isInherited ? t('legalPages.platformDefault') : t('legalPages.customized')}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted-foreground pt-1">
                      {t('legalPages.lastModified')}: {new Date(page.lastModified).toLocaleDateString()}
                    </p>
                  </div>
                  <div className="flex flex-col gap-1">
                    <Button variant="ghost" size="icon" onClick={() => onEdit(page.id)}>
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => window.open(`/${page.slug === 'terms-of-service' ? 'terms' : 'privacy'}`, '_blank')}
                    >
                      <Eye className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
        {pages.length === 0 && (
          <div className="text-center py-8 text-muted-foreground">
            No legal pages found.
          </div>
        )}
      </CardContent>
    </Card>
  )
}
