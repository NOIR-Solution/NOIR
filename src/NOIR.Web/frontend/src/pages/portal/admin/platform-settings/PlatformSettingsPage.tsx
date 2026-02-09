import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import {
  Settings,
  Mail,
  Send,
  Loader2,
  Check,
  AlertCircle,
  Server,
  FileText,
  Scale,
  Pencil,
  Eye,
  GitFork,
} from 'lucide-react'

import { Button } from '@/components/ui/button'
import { PageHeader } from '@/components/ui/page-header'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Switch } from '@/components/ui/switch'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { usePageContext } from '@/hooks/usePageContext'
import { ApiError } from '@/services/apiClient'
import { formatDisplayName } from '@/lib/utils'

import {
  getSmtpSettings,
  updateSmtpSettings,
  testSmtpConnection,
} from '@/services/platformSettings'

import { getEmailTemplates, type EmailTemplateListDto } from '@/services/emailTemplates'
import { getLegalPages, type LegalPageListDto } from '@/services/legalPages'

// ============================================================================
// SMTP Form Schema Factories
// ============================================================================
const createSmtpSettingsSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    host: z.string().min(1, t('validation.required')),
    port: z.coerce.number().int().min(1).max(65535),
    username: z.string().optional().nullable(),
    password: z.string().optional().nullable(),
    fromEmail: z.string().email(t('validation.invalidEmail')),
    fromName: z.string().min(1, t('validation.required')),
    useSsl: z.boolean(),
  })

type SmtpSettingsFormData = z.infer<ReturnType<typeof createSmtpSettingsSchema>>

const createTestEmailSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    recipientEmail: z.string().email(t('validation.invalidEmail')),
  })

type TestEmailFormData = z.infer<ReturnType<typeof createTestEmailSchema>>

// ============================================================================
// Main Component
// ============================================================================
export default function PlatformSettingsPage() {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  usePageContext('PlatformSettings')

  // Active tab state
  const [activeTab, setActiveTab] = useState('smtp')

  return (
    <div className="container max-w-4xl py-6 space-y-6">
      <PageHeader
        icon={Settings}
        title={t('platformSettings.title')}
        description={t('platformSettings.description')}
      />

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
        <TabsList>
          <TabsTrigger value="smtp" className="cursor-pointer">
            <Mail className="h-4 w-4 mr-2" />
            {t('platformSettings.tabs.smtp')}
          </TabsTrigger>
          <TabsTrigger value="emailTemplates" className="cursor-pointer">
            <FileText className="h-4 w-4 mr-2" />
            {t('platformSettings.tabs.emailTemplates')}
          </TabsTrigger>
          <TabsTrigger value="legalPages" className="cursor-pointer">
            <Scale className="h-4 w-4 mr-2" />
            {t('platformSettings.tabs.legalPages')}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="smtp">
          <SmtpSettingsTab />
        </TabsContent>

        <TabsContent value="emailTemplates">
          <EmailTemplatesTab onEdit={(id) => navigate(`/portal/email-templates/${id}`)} />
        </TabsContent>

        <TabsContent value="legalPages">
          <LegalPagesTab onEdit={(id) => navigate(`/portal/legal-pages/${id}`)} />
        </TabsContent>
      </Tabs>
    </div>
  )
}

// ============================================================================
// SMTP Settings Tab
// ============================================================================
function SmtpSettingsTab() {
  const { t } = useTranslation('common')

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [testing, setTesting] = useState(false)
  const [testDialogOpen, setTestDialogOpen] = useState(false)
  const [isConfigured, setIsConfigured] = useState(false)
  const [hasPassword, setHasPassword] = useState(false)

  const form = useForm<SmtpSettingsFormData>({
    // TypeScript cannot infer that dynamic schema factories produce compatible resolver types
    // Using 'as any' is a pragmatic workaround for this limitation
    resolver: zodResolver(createSmtpSettingsSchema(t)) as any,
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
    // TypeScript cannot infer that dynamic schema factories produce compatible resolver types
    // Using 'as any' is a pragmatic workaround for this limitation
    resolver: zodResolver(createTestEmailSchema(t)) as any,
    mode: 'onBlur',
    defaultValues: {
      recipientEmail: '',
    },
  })

  useEffect(() => {
    const loadSettings = async () => {
      try {
        const settings = await getSmtpSettings()
        setIsConfigured(settings.isConfigured)
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

  const onSubmit = async (data: SmtpSettingsFormData) => {
    setSaving(true)
    try {
      const result = await updateSmtpSettings({
        host: data.host,
        port: data.port,
        username: data.username || null,
        password: data.password || null,
        fromEmail: data.fromEmail,
        fromName: data.fromName,
        useSsl: data.useSsl,
      })

      setIsConfigured(result.isConfigured)
      setHasPassword(result.hasPassword)
      form.setValue('password', '')

      toast.success(t('platformSettings.smtp.saveSuccess'))
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to save settings'
      toast.error(message)
    } finally {
      setSaving(false)
    }
  }

  const onTestSubmit = async (data: TestEmailFormData) => {
    setTesting(true)
    try {
      await testSmtpConnection({ recipientEmail: data.recipientEmail })
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
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
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
            <Badge variant={isConfigured ? 'default' : 'secondary'}>
              {isConfigured ? (
                <>
                  <Check className="h-3 w-3 mr-1" />
                  Configured
                </>
              ) : (
                <>
                  <Server className="h-3 w-3 mr-1" />
                  Using defaults
                </>
              )}
            </Badge>
          </div>
        </CardHeader>
        <CardContent>
          {!isConfigured && (
            <Alert className="mb-6">
              <AlertCircle className="h-4 w-4" />
              <AlertTitle>Using Default Configuration</AlertTitle>
              <AlertDescription>
                {t('platformSettings.smtp.notConfigured')}
              </AlertDescription>
            </Alert>
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
                        <Input placeholder={t('platformSettings.smtp.hostPlaceholder')} {...field} />
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
                        <Input type="number" placeholder="587" {...field} />
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
                      <Switch checked={field.value} onCheckedChange={field.onChange} />
                    </FormControl>
                  </FormItem>
                )}
              />

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
// Email Templates Tab
// ============================================================================
function EmailTemplatesTab({ onEdit }: { onEdit: (id: string) => void }) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(true)
  const [templates, setTemplates] = useState<EmailTemplateListDto[]>([])

  useEffect(() => {
    const loadTemplates = async () => {
      try {
        const data = await getEmailTemplates()
        // Filter to only platform templates (isInherited = true means it's a platform default)
        setTemplates(data.filter(t => t.isInherited))
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
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
      <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
        <CardTitle className="text-lg">{t('emailTemplates.title')}</CardTitle>
        <CardDescription>{t('emailTemplates.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 md:grid-cols-2">
          {templates.map((template) => (
            <Card key={template.id} className="overflow-hidden shadow-sm hover:shadow-md transition-all duration-300">
              <CardContent className="p-4">
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <h4 className="font-medium">{formatDisplayName(template.name)}</h4>
                    <p className="text-sm text-muted-foreground line-clamp-2">
                      {template.description}
                    </p>
                    <div className="flex items-center gap-2 pt-2">
                      <Badge variant={template.isActive ? 'default' : 'secondary'} className="text-xs">
                        {template.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                      <Badge variant="outline" className="text-purple-600 border-purple-600/30 text-xs">
                        <GitFork className="h-3 w-3 mr-1" />
                        Platform
                      </Badge>
                    </div>
                  </div>
                  <Button variant="ghost" size="icon" onClick={() => onEdit(template.id)}>
                    <Pencil className="h-4 w-4" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
        {templates.length === 0 && (
          <div className="text-center py-8 text-muted-foreground">
            No platform email templates found.
          </div>
        )}
      </CardContent>
    </Card>
  )
}

// ============================================================================
// Legal Pages Tab
// ============================================================================
function LegalPagesTab({ onEdit }: { onEdit: (id: string) => void }) {
  const { t } = useTranslation('common')
  const { formatDate } = useRegionalSettings()
  const [loading, setLoading] = useState(true)
  const [pages, setPages] = useState<LegalPageListDto[]>([])

  useEffect(() => {
    const loadPages = async () => {
      try {
        const data = await getLegalPages()
        // Filter to only platform pages (isInherited = true means it's a platform default)
        setPages(data.filter(p => p.isInherited))
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
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
      <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
        <CardTitle className="text-lg">{t('legalPages.title')}</CardTitle>
        <CardDescription>{t('legalPages.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 md:grid-cols-2">
          {pages.map((page) => (
            <Card key={page.id} className="overflow-hidden shadow-sm hover:shadow-md transition-all duration-300">
              <CardContent className="p-4">
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <h4 className="font-medium">{page.title}</h4>
                    <p className="text-sm text-muted-foreground">/{page.slug}</p>
                    <div className="flex items-center gap-2 pt-2">
                      <Badge variant="outline" className="text-purple-600 border-purple-600/30 text-xs">
                        <GitFork className="h-3 w-3 mr-1" />
                        Platform Default
                      </Badge>
                    </div>
                    <p className="text-xs text-muted-foreground pt-1">
                      Last modified: {formatDate(page.lastModified)}
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
            No platform legal pages found.
          </div>
        )}
      </CardContent>
    </Card>
  )
}
