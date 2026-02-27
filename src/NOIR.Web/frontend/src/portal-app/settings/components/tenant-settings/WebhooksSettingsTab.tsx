import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { toast } from 'sonner'
import {
  Webhook,
  Plus,
  Pencil,
  Trash2,
  Play,
  PowerOff,
  Power,
  RefreshCw,
  ScrollText,
  Loader2,
  CheckCircle2,
  XCircle,
  Clock,
  AlertCircle,
} from 'lucide-react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  EmptyState,
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Textarea,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import {
  useWebhooks,
  useWebhookById,
  useWebhookDeliveryLogs,
  useCreateWebhook,
  useUpdateWebhook,
  useDeleteWebhook,
  useActivateWebhook,
  useDeactivateWebhook,
  useTestWebhook,
  useRotateWebhookSecret,
} from '@/hooks/useWebhooks'
import type {
  WebhookSubscriptionSummaryDto,
  WebhookSubscriptionDto,
  WebhookDeliveryStatus,
  CreateWebhookRequest,
} from '@/types/webhook'

// ============================================================================
// Helpers
// ============================================================================

const getWebhookStatusBadgeColor = (status: string) => {
  switch (status) {
    case 'Active':
      return getStatusBadgeClasses('green')
    case 'Inactive':
      return getStatusBadgeClasses('gray')
    case 'Suspended':
      return getStatusBadgeClasses('red')
    default:
      return getStatusBadgeClasses('gray')
  }
}

const getDeliveryStatusIcon = (status: WebhookDeliveryStatus) => {
  switch (status) {
    case 'Succeeded':
      return <CheckCircle2 className="h-4 w-4 text-green-500" />
    case 'Failed':
      return <XCircle className="h-4 w-4 text-red-500" />
    case 'Retrying':
      return <RefreshCw className="h-4 w-4 text-yellow-500 animate-spin" />
    case 'Exhausted':
      return <AlertCircle className="h-4 w-4 text-red-600" />
    case 'Pending':
    default:
      return <Clock className="h-4 w-4 text-muted-foreground" />
  }
}

const getDeliveryStatusBadgeColor = (status: WebhookDeliveryStatus) => {
  switch (status) {
    case 'Succeeded':
      return getStatusBadgeClasses('green')
    case 'Failed':
    case 'Exhausted':
      return getStatusBadgeClasses('red')
    case 'Retrying':
      return getStatusBadgeClasses('yellow')
    case 'Pending':
    default:
      return getStatusBadgeClasses('gray')
  }
}

const formatDate = (dateStr: string | null) => {
  if (!dateStr) return '—'
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(dateStr))
}

const calcSuccessRate = (webhook: WebhookSubscriptionSummaryDto): string => {
  if (webhook.totalDeliveries === 0) return '—'
  const rate = Math.round((webhook.successfulDeliveries / webhook.totalDeliveries) * 100)
  return `${rate}%`
}

// ============================================================================
// Form Schema
// ============================================================================

const createWebhookSchema = (t: (key: string) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength')),
    url: z.string().url(t('validation.invalidUrl')),
    eventPatterns: z.string().min(1, t('validation.required')),
    description: z.string().max(500, t('validation.maxLength')).optional().or(z.literal('')),
    customHeaders: z.string().optional().or(z.literal('')),
    maxRetries: z.number().int().min(0).max(10).optional(),
    timeoutSeconds: z.number().int().min(5).max(120).optional(),
  })

type WebhookFormData = z.infer<ReturnType<typeof createWebhookSchema>>

// ============================================================================
// Webhook Form Dialog
// ============================================================================

interface WebhookDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  webhook?: WebhookSubscriptionSummaryDto
}

const WebhookDialog = ({ open, onOpenChange, webhook }: WebhookDialogProps) => {
  const { t } = useTranslation('common')
  const isEditing = !!webhook
  const { data: fullWebhook } = useWebhookById(isEditing && open ? webhook?.id : undefined)
  const createMutation = useCreateWebhook()
  const updateMutation = useUpdateWebhook()

  const schema = createWebhookSchema(t)
  const form = useForm<WebhookFormData>({
    resolver: zodResolver(schema) as unknown as Resolver<WebhookFormData>,
    mode: 'onBlur',
    defaultValues: {
      name: '',
      url: '',
      eventPatterns: '',
      description: '',
      customHeaders: '',
      maxRetries: 3,
      timeoutSeconds: 30,
    },
  })

  useEffect(() => {
    if (open) {
      // In edit mode, prefer the full DTO which includes description, customHeaders, etc.
      const source: Partial<WebhookSubscriptionDto> | undefined = fullWebhook ?? webhook
      form.reset({
        name: source?.name ?? '',
        url: source?.url ?? '',
        eventPatterns: source?.eventPatterns ?? '*',
        description: (source as WebhookSubscriptionDto | undefined)?.description ?? '',
        customHeaders: (source as WebhookSubscriptionDto | undefined)?.customHeaders ?? '',
        maxRetries: (source as WebhookSubscriptionDto | undefined)?.maxRetries ?? 3,
        timeoutSeconds: (source as WebhookSubscriptionDto | undefined)?.timeoutSeconds ?? 30,
      })
    }
  }, [open, webhook, fullWebhook, form])

  const handleSubmit = async (data: WebhookFormData) => {
    const request: CreateWebhookRequest = {
      name: data.name,
      url: data.url,
      eventPatterns: data.eventPatterns,
      description: data.description || undefined,
      customHeaders: data.customHeaders || undefined,
      maxRetries: data.maxRetries,
      timeoutSeconds: data.timeoutSeconds,
    }

    try {
      if (isEditing && webhook) {
        await updateMutation.mutateAsync({ id: webhook.id, request: { ...request, id: webhook.id } })
        toast.success(t('webhooks.updated'))
      } else {
        await createMutation.mutateAsync(request)
        toast.success(t('webhooks.created'))
      }
      onOpenChange(false)
      form.reset()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.unexpectedError'))
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[560px]">
        <CredenzaHeader>
          <CredenzaTitle>
            {isEditing ? t('webhooks.editWebhook') : t('webhooks.createWebhook')}
          </CredenzaTitle>
          <CredenzaDescription>
            {t('webhooks.description')}
          </CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4" id="webhook-form">
              <FormField
                control={form.control}
                name="name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('webhooks.name')}</FormLabel>
                    <FormControl>
                      <Input {...field} placeholder={t('webhooks.name')} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="url"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('webhooks.url')}</FormLabel>
                    <FormControl>
                      <Input {...field} placeholder={t('webhooks.urlPlaceholder')} type="url" />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="eventPatterns"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('webhooks.eventPatterns')}</FormLabel>
                    <FormControl>
                      <Input {...field} placeholder={t('webhooks.eventPatternsPlaceholder')} />
                    </FormControl>
                    <FormDescription>{t('webhooks.eventPatternsHelp')}</FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="description"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('webhooks.description')}</FormLabel>
                    <FormControl>
                      <Textarea {...field} rows={2} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="maxRetries"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('webhooks.maxRetries')}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          type="number"
                          min={0}
                          max={10}
                          onChange={(e) => field.onChange(parseInt(e.target.value, 10) || 0)}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="timeoutSeconds"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('webhooks.timeoutSeconds')}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          type="number"
                          min={5}
                          max={120}
                          onChange={(e) => field.onChange(parseInt(e.target.value, 10) || 30)}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
              <FormField
                control={form.control}
                name="customHeaders"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('webhooks.customHeaders')}</FormLabel>
                    <FormControl>
                      <Textarea {...field} rows={3} placeholder='{"X-Custom-Header": "value"}' />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </form>
          </Form>
        </CredenzaBody>
        <CredenzaFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isPending}
            className="cursor-pointer"
          >
            {t('buttons.cancel')}
          </Button>
          <Button
            type="submit"
            form="webhook-form"
            disabled={isPending}
            className="cursor-pointer transition-all duration-300"
          >
            {isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            {isEditing ? t('buttons.save') : t('buttons.create')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}

// ============================================================================
// Delete Confirmation Dialog
// ============================================================================

interface DeleteWebhookDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  webhook: WebhookSubscriptionSummaryDto | null
}

const DeleteWebhookDialog = ({ open, onOpenChange, webhook }: DeleteWebhookDialogProps) => {
  const { t } = useTranslation('common')
  const deleteMutation = useDeleteWebhook()

  const handleDelete = async () => {
    if (!webhook) return
    try {
      await deleteMutation.mutateAsync(webhook.id)
      toast.success(t('webhooks.deleted'))
      onOpenChange(false)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.unexpectedError'))
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[440px] border-destructive/30">
        <CredenzaHeader>
          <CredenzaTitle>{t('webhooks.deleteWebhook')}</CredenzaTitle>
          <CredenzaDescription>{t('webhooks.deleteConfirmation')}</CredenzaDescription>
        </CredenzaHeader>
        {webhook && (
          <CredenzaBody>
            <p className="text-sm font-medium">{webhook.name}</p>
            <p className="text-xs text-muted-foreground break-all">{webhook.url}</p>
          </CredenzaBody>
        )}
        <CredenzaFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={deleteMutation.isPending}
            className="cursor-pointer"
          >
            {t('buttons.cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleDelete}
            disabled={deleteMutation.isPending}
            className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
          >
            {deleteMutation.isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            {t('buttons.delete')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}

// ============================================================================
// Rotate Secret Confirmation Dialog
// ============================================================================

interface RotateSecretDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  webhook: WebhookSubscriptionSummaryDto | null
}

const RotateSecretDialog = ({ open, onOpenChange, webhook }: RotateSecretDialogProps) => {
  const { t } = useTranslation('common')
  const rotateMutation = useRotateWebhookSecret()

  const handleRotate = async () => {
    if (!webhook) return
    try {
      const result = await rotateMutation.mutateAsync(webhook.id)
      toast.success(t('webhooks.secretRotated'))
      try {
        await navigator.clipboard.writeText(result.secret)
        toast.info(t('webhooks.secretCopied'))
      } catch {
        // Clipboard API may fail (insecure context, denied permission, etc.)
        toast.info(result.secret, { duration: 15000 })
      }
      onOpenChange(false)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.unexpectedError'))
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[440px] border-destructive/30">
        <CredenzaHeader>
          <CredenzaTitle>{t('webhooks.rotateSecret')}</CredenzaTitle>
          <CredenzaDescription>{t('webhooks.rotateSecretConfirmation')}</CredenzaDescription>
        </CredenzaHeader>
        <CredenzaFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={rotateMutation.isPending}
            className="cursor-pointer"
          >
            {t('buttons.cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleRotate}
            disabled={rotateMutation.isPending}
            className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
          >
            {rotateMutation.isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            {t('webhooks.rotateSecret')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}

// ============================================================================
// Delivery Logs Dialog
// ============================================================================

interface DeliveryLogsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  webhook: WebhookSubscriptionSummaryDto | null
}

const DeliveryLogsDialog = ({ open, onOpenChange, webhook }: DeliveryLogsDialogProps) => {
  const { t } = useTranslation('common')
  const { data, isLoading } = useWebhookDeliveryLogs(open ? webhook?.id : undefined, { pageSize: 20 })

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[720px]">
        <CredenzaHeader>
          <CredenzaTitle>{t('webhooks.deliveryLogs')}</CredenzaTitle>
          <CredenzaDescription>{webhook?.name}</CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody>
          {isLoading ? (
            <div className="space-y-2">
              {[1, 2, 3].map((i) => (
                <Skeleton key={i} className="h-12 w-full" />
              ))}
            </div>
          ) : !data?.items?.length ? (
            <EmptyState
              icon={ScrollText}
              title={t('webhooks.noDeliveryLogs')}
              description={t('webhooks.noDeliveryLogsDescription')}
              size="sm"
            />
          ) : (
            <div className="overflow-auto max-h-[400px]">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-8"></TableHead>
                    <TableHead>{t('webhooks.eventType')}</TableHead>
                    <TableHead>{t('webhooks.responseStatus')}</TableHead>
                    <TableHead>{t('webhooks.duration')}</TableHead>
                    <TableHead>{t('webhooks.attempt')}</TableHead>
                    <TableHead>{t('labels.createdAt')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((log) => (
                    <TableRow key={log.id}>
                      <TableCell>{getDeliveryStatusIcon(log.status)}</TableCell>
                      <TableCell className="font-mono text-xs">{log.eventType}</TableCell>
                      <TableCell>
                        {log.responseStatusCode != null ? (
                          <Badge
                            variant="outline"
                            className={getDeliveryStatusBadgeColor(log.status)}
                          >
                            {log.responseStatusCode}
                          </Badge>
                        ) : (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </TableCell>
                      <TableCell className="text-muted-foreground text-sm">
                        {log.durationMs != null ? `${log.durationMs}ms` : '—'}
                      </TableCell>
                      <TableCell className="text-muted-foreground text-sm">#{log.attemptNumber}</TableCell>
                      <TableCell className="text-muted-foreground text-sm whitespace-nowrap">
                        {formatDate(log.createdAt)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CredenzaBody>
        <CredenzaFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            className="cursor-pointer"
          >
            {t('buttons.close')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}

// ============================================================================
// Main Tab Component
// ============================================================================

interface WebhooksSettingsTabProps {
  canEdit: boolean
}

export const WebhooksSettingsTab = ({ canEdit }: WebhooksSettingsTabProps) => {
  const { t } = useTranslation('common')
  const { data, isLoading } = useWebhooks({ pageSize: 50 })
  const activateMutation = useActivateWebhook()
  const deactivateMutation = useDeactivateWebhook()
  const testMutation = useTestWebhook()

  const [createOpen, setCreateOpen] = useState(false)
  const [editWebhook, setEditWebhook] = useState<WebhookSubscriptionSummaryDto | null>(null)
  const [deleteWebhook, setDeleteWebhook] = useState<WebhookSubscriptionSummaryDto | null>(null)
  const [rotateSecretWebhook, setRotateSecretWebhook] = useState<WebhookSubscriptionSummaryDto | null>(null)
  const [logsWebhook, setLogsWebhook] = useState<WebhookSubscriptionSummaryDto | null>(null)
  const [testingId, setTestingId] = useState<string | null>(null)
  const [togglingId, setTogglingId] = useState<string | null>(null)

  const handleToggleActive = async (webhook: WebhookSubscriptionSummaryDto) => {
    setTogglingId(webhook.id)
    try {
      if (webhook.isActive) {
        await deactivateMutation.mutateAsync(webhook.id)
        toast.success(t('webhooks.deactivated'))
      } else {
        await activateMutation.mutateAsync(webhook.id)
        toast.success(t('webhooks.activated'))
      }
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.unexpectedError'))
    } finally {
      setTogglingId(null)
    }
  }

  const handleTest = async (webhook: WebhookSubscriptionSummaryDto) => {
    setTestingId(webhook.id)
    try {
      await testMutation.mutateAsync(webhook.id)
      toast.success(t('webhooks.testSent'))
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.unexpectedError'))
    } finally {
      setTestingId(null)
    }
  }

  if (isLoading) {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="space-y-1">
              <Skeleton className="h-6 w-32" />
              <Skeleton className="h-4 w-80" />
            </div>
            <Skeleton className="h-9 w-32" />
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {[1, 2, 3].map((i) => (
              <Skeleton key={i} className="h-14 w-full" />
            ))}
          </div>
        </CardContent>
      </Card>
    )
  }

  const webhooks = data?.items ?? []

  return (
    <>
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-lg">{t('webhooks.title')}</CardTitle>
              <CardDescription>{t('webhooks.description')}</CardDescription>
            </div>
            {canEdit && (
              <Button
                onClick={() => setCreateOpen(true)}
                className="cursor-pointer group transition-all duration-300"
              >
                <Plus className="h-4 w-4 mr-2" />
                {t('webhooks.createWebhook')}
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {webhooks.length === 0 ? (
            <EmptyState
              icon={Webhook}
              title={t('webhooks.noWebhooks')}
              description={t('webhooks.noWebhooksDescription')}
              action={canEdit ? {
                label: t('webhooks.createWebhook'),
                onClick: () => setCreateOpen(true),
              } : undefined}
            />
          ) : (
            <TooltipProvider>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('webhooks.name')}</TableHead>
                    <TableHead>{t('webhooks.url')}</TableHead>
                    <TableHead>{t('webhooks.status')}</TableHead>
                    <TableHead>{t('webhooks.eventPatterns')}</TableHead>
                    <TableHead>{t('webhooks.lastDelivery')}</TableHead>
                    <TableHead>{t('webhooks.successRate')}</TableHead>
                    <TableHead className="text-right">{t('labels.actions')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {webhooks.map((webhook) => (
                    <TableRow key={webhook.id}>
                      <TableCell className="font-medium">{webhook.name}</TableCell>
                      <TableCell>
                        <span className="text-xs font-mono text-muted-foreground max-w-[200px] truncate block">
                          {webhook.url}
                        </span>
                      </TableCell>
                      <TableCell>
                        <Badge
                          variant="outline"
                          className={getWebhookStatusBadgeColor(webhook.status)}
                        >
                          {t(`webhooks.statuses.${webhook.status}`)}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <span className="text-xs font-mono text-muted-foreground max-w-[160px] truncate block">
                          {webhook.eventPatterns}
                        </span>
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground whitespace-nowrap">
                        {formatDate(webhook.lastDeliveryAt)}
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {calcSuccessRate(webhook)}
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center justify-end gap-1">
                          {/* Test */}
                          {canEdit && (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  onClick={() => handleTest(webhook)}
                                  disabled={testingId === webhook.id}
                                  className="cursor-pointer h-8 w-8"
                                  aria-label={`${t('webhooks.testWebhook')} ${webhook.name}`}
                                >
                                  {testingId === webhook.id ? (
                                    <Loader2 className="h-4 w-4 animate-spin" />
                                  ) : (
                                    <Play className="h-4 w-4" />
                                  )}
                                </Button>
                              </TooltipTrigger>
                              <TooltipContent>{t('webhooks.testWebhook')}</TooltipContent>
                            </Tooltip>
                          )}

                          {/* View Logs */}
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => setLogsWebhook(webhook)}
                                className="cursor-pointer h-8 w-8"
                                aria-label={`${t('webhooks.deliveryLogs')} ${webhook.name}`}
                              >
                                <ScrollText className="h-4 w-4" />
                              </Button>
                            </TooltipTrigger>
                            <TooltipContent>{t('webhooks.deliveryLogs')}</TooltipContent>
                          </Tooltip>

                          {/* Rotate Secret */}
                          {canEdit && (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  onClick={() => setRotateSecretWebhook(webhook)}
                                  className="cursor-pointer h-8 w-8"
                                  aria-label={`${t('webhooks.rotateSecret')} ${webhook.name}`}
                                >
                                  <RefreshCw className="h-4 w-4" />
                                </Button>
                              </TooltipTrigger>
                              <TooltipContent>{t('webhooks.rotateSecret')}</TooltipContent>
                            </Tooltip>
                          )}

                          {/* Activate / Deactivate */}
                          {canEdit && (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  onClick={() => handleToggleActive(webhook)}
                                  disabled={togglingId === webhook.id}
                                  className="cursor-pointer h-8 w-8"
                                  aria-label={`${webhook.isActive ? t('webhooks.deactivate') : t('webhooks.activate')} ${webhook.name}`}
                                >
                                  {togglingId === webhook.id ? (
                                    <Loader2 className="h-4 w-4 animate-spin" />
                                  ) : webhook.isActive ? (
                                    <PowerOff className="h-4 w-4" />
                                  ) : (
                                    <Power className="h-4 w-4" />
                                  )}
                                </Button>
                              </TooltipTrigger>
                              <TooltipContent>
                                {webhook.isActive ? t('webhooks.deactivate') : t('webhooks.activate')}
                              </TooltipContent>
                            </Tooltip>
                          )}

                          {/* Edit */}
                          {canEdit && (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  onClick={() => setEditWebhook(webhook)}
                                  className="cursor-pointer h-8 w-8"
                                  aria-label={`${t('webhooks.editWebhook')} ${webhook.name}`}
                                >
                                  <Pencil className="h-4 w-4" />
                                </Button>
                              </TooltipTrigger>
                              <TooltipContent>{t('webhooks.editWebhook')}</TooltipContent>
                            </Tooltip>
                          )}

                          {/* Delete */}
                          {canEdit && (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  onClick={() => setDeleteWebhook(webhook)}
                                  className="cursor-pointer h-8 w-8 text-destructive hover:text-destructive hover:bg-destructive/10"
                                  aria-label={`${t('webhooks.deleteWebhook')} ${webhook.name}`}
                                >
                                  <Trash2 className="h-4 w-4" />
                                </Button>
                              </TooltipTrigger>
                              <TooltipContent>{t('webhooks.deleteWebhook')}</TooltipContent>
                            </Tooltip>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TooltipProvider>
          )}
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <WebhookDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
      />

      {/* Edit Dialog */}
      {editWebhook && (
        <WebhookDialog
          open={!!editWebhook}
          onOpenChange={(open) => { if (!open) setEditWebhook(null) }}
          webhook={editWebhook}
        />
      )}

      {/* Delete Dialog */}
      <DeleteWebhookDialog
        open={!!deleteWebhook}
        onOpenChange={(open) => { if (!open) setDeleteWebhook(null) }}
        webhook={deleteWebhook}
      />

      {/* Rotate Secret Dialog */}
      <RotateSecretDialog
        open={!!rotateSecretWebhook}
        onOpenChange={(open) => { if (!open) setRotateSecretWebhook(null) }}
        webhook={rotateSecretWebhook}
      />

      {/* Delivery Logs Dialog */}
      <DeliveryLogsDialog
        open={!!logsWebhook}
        onOpenChange={(open) => { if (!open) setLogsWebhook(null) }}
        webhook={logsWebhook}
      />
    </>
  )
}
