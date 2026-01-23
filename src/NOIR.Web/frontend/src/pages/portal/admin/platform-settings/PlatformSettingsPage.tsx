import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { Settings, Mail, Send, Loader2, Check, AlertCircle, Server } from 'lucide-react'

import { Button } from '@/components/ui/button'
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
import { usePageContext } from '@/hooks/usePageContext'
import { ApiError } from '@/services/apiClient'

import {
  getSmtpSettings,
  updateSmtpSettings,
  testSmtpConnection,
  type SmtpSettingsDto,
} from '@/services/platformSettings'

// Validation schema for SMTP settings
const smtpSettingsSchema = z.object({
  host: z.string().min(1, 'SMTP host is required'),
  port: z.coerce.number().int().min(1).max(65535),
  username: z.string().optional().nullable(),
  password: z.string().optional().nullable(),
  fromEmail: z.string().email('Invalid email address'),
  fromName: z.string().min(1, 'From name is required'),
  useSsl: z.boolean(),
})

type SmtpSettingsFormData = z.infer<typeof smtpSettingsSchema>

// Validation schema for test email
const testEmailSchema = z.object({
  recipientEmail: z.string().email('Invalid email address'),
})

type TestEmailFormData = z.infer<typeof testEmailSchema>

export default function PlatformSettingsPage() {
  const { t } = useTranslation('common')

  // Set page context for Activity Timeline
  usePageContext('PlatformSettings')

  // State
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [testing, setTesting] = useState(false)
  const [testDialogOpen, setTestDialogOpen] = useState(false)
  const [isConfigured, setIsConfigured] = useState(false)
  const [hasPassword, setHasPassword] = useState(false)

  // SMTP settings form
  const form = useForm<SmtpSettingsFormData>({
    resolver: zodResolver(smtpSettingsSchema),
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

  // Test email form
  const testForm = useForm<TestEmailFormData>({
    resolver: zodResolver(testEmailSchema),
    defaultValues: {
      recipientEmail: '',
    },
  })

  // Load settings on mount
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
          password: '', // Never expose password
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

  // Handle save
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

      // Clear password field after save (don't show it)
      form.setValue('password', '')

      toast.success(t('platformSettings.smtp.saveSuccess'))
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to save settings'
      toast.error(message)
    } finally {
      setSaving(false)
    }
  }

  // Handle test email
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
      <div className="container max-w-4xl py-6">
        <div className="flex items-center justify-center h-64">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      </div>
    )
  }

  return (
    <div className="container max-w-4xl py-6 space-y-6">
      {/* Page Header */}
      <div className="flex items-center gap-3">
        <div className="h-10 w-10 rounded-lg bg-primary/10 flex items-center justify-center">
          <Settings className="h-5 w-5 text-primary" />
        </div>
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{t('platformSettings.title')}</h1>
          <p className="text-muted-foreground">{t('platformSettings.description')}</p>
        </div>
      </div>

      {/* SMTP Settings Card */}
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
          {/* Status Alert */}
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
              {/* Server Settings */}
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
                        <Input
                          type="number"
                          placeholder="587"
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Authentication */}
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

              {/* Sender Settings */}
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

              {/* SSL Toggle */}
              <FormField
                control={form.control}
                name="useSsl"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
                    <div className="space-y-0.5">
                      <FormLabel className="text-base">
                        {t('platformSettings.smtp.useSsl')}
                      </FormLabel>
                      <FormDescription>
                        {t('platformSettings.smtp.useSslHint')}
                      </FormDescription>
                    </div>
                    <FormControl>
                      <Switch
                        checked={field.value}
                        onCheckedChange={field.onChange}
                      />
                    </FormControl>
                  </FormItem>
                )}
              />

              {/* Actions */}
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
                      Saving...
                    </>
                  ) : (
                    'Save Settings'
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
            <DialogDescription>
              {t('platformSettings.smtp.testConnectionDescription')}
            </DialogDescription>
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
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setTestDialogOpen(false)}
                  disabled={testing}
                >
                  Cancel
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
    </div>
  )
}
