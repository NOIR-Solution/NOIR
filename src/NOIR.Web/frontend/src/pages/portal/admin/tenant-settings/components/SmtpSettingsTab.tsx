import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import {
  Mail,
  Send,
  Loader2,
  Check,
  GitFork,
  Info,
  RotateCcw,
} from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Switch } from '@/components/ui/switch'
import { Badge } from '@/components/ui/badge'
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
import { ApiError } from '@/services/apiClient'
import {
  getTenantSmtpSettings,
  updateTenantSmtpSettings,
  revertTenantSmtpSettings,
  testTenantSmtpConnection,
} from '@/services/tenantSettings'

// ============================================================================
// Form Schema Factories
// ============================================================================
const createTenantSmtpSettingsSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    host: z.string().min(1, t('validation.required')),
    port: z.coerce.number().int().min(1).max(65535),
    username: z.string().optional().nullable(),
    password: z.string().optional().nullable(),
    fromEmail: z.string().email(t('validation.invalidEmail')),
    fromName: z.string().min(1, t('validation.required')),
    useSsl: z.boolean(),
  })

type TenantSmtpFormData = z.infer<ReturnType<typeof createTenantSmtpSettingsSchema>>

const createTestEmailSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    recipientEmail: z.string().email(t('validation.invalidEmail')),
  })

type TestEmailFormData = z.infer<ReturnType<typeof createTestEmailSchema>>

export interface SmtpSettingsTabProps {
  canEdit: boolean
}

export function SmtpSettingsTab({ canEdit }: SmtpSettingsTabProps) {
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
    // TypeScript cannot infer that dynamic schema factories produce compatible resolver types
    // Using 'as any' is a pragmatic workaround for this limitation
    resolver: zodResolver(createTenantSmtpSettingsSchema(t)) as any,
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
