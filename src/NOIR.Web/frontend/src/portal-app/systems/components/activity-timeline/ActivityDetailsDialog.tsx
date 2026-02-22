import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useRegionalSettings, getLocaleForFormat } from '@/contexts/RegionalSettingsContext'
import {
  Clock,
  User,
  Globe,
  Code,
  ArrowRight,
  CheckCircle2,
  XCircle,
  AlertCircle,
  Database,
  FileJson,
  Minus,
  Plus,
  Fingerprint,
  Terminal,
  Hash,
  Copy,
  Check,
} from 'lucide-react'
import {
  Badge,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  DiffViewer,
  HttpMethodBadge,
  JsonViewer,
  ScrollArea,
  Skeleton,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'

import { cn } from '@/lib/utils'
import {
  getActivityDetails,
  type ActivityTimelineEntry,
  type ActivityDetails,
  type FieldChange,
} from '@/services/audit'

interface ActivityDetailsDialogProps {
  entry: ActivityTimelineEntry | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

// Format timestamp for display (uses tenant timezone and date format)
const formatTimestamp = (timestamp: string, timezone: string, dateFormat: string): string => {
  const date = new Date(timestamp)
  const locale = getLocaleForFormat(dateFormat)
  try {
    return date.toLocaleString(locale, {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
      timeZone: timezone,
    })
  } catch {
    return date.toLocaleString(locale, {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    })
  }
}

// Color variants for metadata items
type MetadataVariant = 'blue' | 'purple' | 'amber'

const metadataVariants: Record<MetadataVariant, { bg: string; border: string; icon: string; copiedBg: string }> = {
  blue: {
    bg: 'bg-blue-50 dark:bg-blue-950/40',
    border: 'border-blue-200 dark:border-blue-800',
    icon: 'text-blue-500',
    copiedBg: 'bg-green-50 dark:bg-green-950/30 border-green-500/50',
  },
  purple: {
    bg: 'bg-purple-50 dark:bg-purple-950/40',
    border: 'border-purple-200 dark:border-purple-800',
    icon: 'text-purple-500',
    copiedBg: 'bg-green-50 dark:bg-green-950/30 border-green-500/50',
  },
  amber: {
    bg: 'bg-amber-50 dark:bg-amber-950/40',
    border: 'border-amber-200 dark:border-amber-800',
    icon: 'text-amber-600',
    copiedBg: 'bg-green-50 dark:bg-green-950/30 border-green-500/50',
  },
}

// Copyable metadata item with click-to-copy functionality
const CopyableMetadata = ({
  icon: Icon,
  label,
  value,
  variant = 'blue',
  maxWidth = 'max-w-[140px]',
}: {
  icon: React.ElementType
  label: string
  value: string
  variant?: MetadataVariant
  maxWidth?: string
}) => {
  const [copied, setCopied] = useState(false)
  const colors = metadataVariants[variant]

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(value)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // Silently fail - clipboard may not be available
    }
  }

  return (
    <button
      type="button"
      onClick={handleCopy}
      className={cn(
        'flex items-center gap-1.5 px-2 py-1 rounded-md border transition-colors group cursor-pointer min-w-0',
        copied ? colors.copiedBg : `${colors.bg} ${colors.border} hover:opacity-80`
      )}
      title={`Click to copy: ${value}`}
    >
      <Icon className={cn('h-3.5 w-3.5 flex-shrink-0', copied ? 'text-green-500' : colors.icon)} />
      <span className="text-xs text-muted-foreground flex-shrink-0">{label}:</span>
      <code className={cn('font-mono text-xs font-medium truncate', maxWidth)}>{value}</code>
      {copied ? (
        <Check className="h-3 w-3 text-green-500 flex-shrink-0" />
      ) : (
        <Copy className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0" />
      )}
    </button>
  )
}

// Component to display a field change
const FieldChangeItem = ({ change }: { change: FieldChange }) => {
  const { t } = useTranslation('common')
  return (
    <div className="p-3 rounded-lg border bg-card">
      <div className="flex items-center gap-2 mb-2">
        <Badge
          variant={change.operation === 'Added' ? 'default' : change.operation === 'Removed' ? 'destructive' : 'secondary'}
          className="text-xs"
        >
          {change.operation === 'Added' && <Plus className="h-3 w-3 mr-1" />}
          {change.operation === 'Removed' && <Minus className="h-3 w-3 mr-1" />}
          {change.operation}
        </Badge>
        <span className="font-mono text-sm font-medium">{change.fieldName}</span>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-2 text-sm">
        {change.operation !== 'Added' && (
          <div className="p-2 rounded bg-red-50 dark:bg-red-950 border border-red-200 dark:border-red-800">
            <span className="text-xs text-muted-foreground block mb-1">{t('activityTimeline.oldValue')}:</span>
            <code className="text-xs break-all">
              {change.oldValue !== null && change.oldValue !== undefined
                ? typeof change.oldValue === 'object'
                  ? JSON.stringify(change.oldValue, null, 2)
                  : String(change.oldValue)
                : t('activityTimeline.null')}
            </code>
          </div>
        )}
        {change.operation !== 'Removed' && (
          <div className="p-2 rounded bg-green-50 dark:bg-green-950 border border-green-200 dark:border-green-800">
            <span className="text-xs text-muted-foreground block mb-1">{t('activityTimeline.newValue')}:</span>
            <code className="text-xs break-all">
              {change.newValue !== null && change.newValue !== undefined
                ? typeof change.newValue === 'object'
                  ? JSON.stringify(change.newValue, null, 2)
                  : String(change.newValue)
                : t('activityTimeline.null')}
            </code>
          </div>
        )}
      </div>
    </div>
  )
}

export const ActivityDetailsDialog = ({
  entry,
  open,
  onOpenChange,
}: ActivityDetailsDialogProps) => {
  const { t } = useTranslation('common')
  const [details, setDetails] = useState<ActivityDetails | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const { timezone, dateFormat } = useRegionalSettings()

  useEffect(() => {
    if (entry && open) {
      setLoading(true)
      setError(null)
      getActivityDetails(entry.id)
        .then(setDetails)
        .catch((err) => setError(err instanceof Error ? err.message : t('activityTimeline.failedToLoadDetails')))
        .finally(() => setLoading(false))
    } else {
      setDetails(null)
    }
  }, [entry, open])

  if (!entry) return null

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="max-w-3xl max-h-[90vh] flex flex-col">
        <CredenzaHeader className="space-y-3">
          <CredenzaTitle className="flex items-center gap-2">
            {entry.isSuccess ? (
              <CheckCircle2 className="h-5 w-5 text-green-500" />
            ) : (
              <XCircle className="h-5 w-5 text-red-500" />
            )}
            {entry.actionDescription || entry.displayContext}
          </CredenzaTitle>
          <CredenzaDescription asChild>
            <div className="space-y-2">
              {/* Basic info row */}
              <div className="flex items-center gap-4 text-sm">
                <span className="flex items-center gap-1">
                  <User className="h-4 w-4" />
                  {entry.userEmail || 'System'}
                </span>
                <span className="flex items-center gap-1">
                  <Clock className="h-4 w-4" />
                  {formatTimestamp(entry.timestamp, timezone, dateFormat)}
                </span>
                <Badge
                  variant="outline"
                  className={cn(
                    'text-xs',
                    entry.operationType === 'Create' && 'text-green-700 dark:text-green-400',
                    entry.operationType === 'Update' && 'text-blue-700 dark:text-blue-400',
                    entry.operationType === 'Delete' && 'text-red-700 dark:text-red-400'
                  )}
                >
                  {entry.operationType}
                </Badge>
              </div>
              {/* Technical metadata row - clickable to copy */}
              <div className="flex items-center gap-2 overflow-hidden">
                {entry.handlerName && (
                  <CopyableMetadata
                    icon={Terminal}
                    label="Handler"
                    value={entry.handlerName}
                    variant="blue"
                    maxWidth="max-w-[180px]"
                  />
                )}
                {entry.targetDtoId && (
                  <CopyableMetadata
                    icon={Hash}
                    label="Entity"
                    value={entry.targetDtoId}
                    variant="purple"
                    maxWidth="max-w-[100px]"
                  />
                )}
                {entry.correlationId && (
                  <CopyableMetadata
                    icon={Fingerprint}
                    label="Corr"
                    value={entry.correlationId}
                    variant="amber"
                    maxWidth="max-w-[100px]"
                  />
                )}
              </div>
            </div>
          </CredenzaDescription>
        </CredenzaHeader>

        <CredenzaBody>
          {loading ? (
            <div className="space-y-4 py-4">
              <Skeleton className="h-8 w-full" />
              <Skeleton className="h-32 w-full" />
              <Skeleton className="h-32 w-full" />
            </div>
          ) : error ? (
            <div className="p-4 bg-destructive/10 text-destructive rounded-lg flex items-center gap-2">
              <AlertCircle className="h-4 w-4" />
              {error}
            </div>
          ) : details ? (
            <Tabs defaultValue="http" className="flex-1">
              <TabsList className="grid grid-cols-4 w-full">
                <TabsTrigger value="http" className="text-xs">
                  <Globe className="h-4 w-4 mr-1" />
                  HTTP
                </TabsTrigger>
                <TabsTrigger value="dto" className="text-xs">
                  <FileJson className="h-4 w-4 mr-1" />
                  Handler
                </TabsTrigger>
                <TabsTrigger value="changes" className="text-xs">
                  <Database className="h-4 w-4 mr-1" />
                  Database ({details.entityChanges.reduce((acc, e) => acc + e.changes.length, 0)})
                </TabsTrigger>
                <TabsTrigger value="raw" className="text-xs">
                  <Code className="h-4 w-4 mr-1" />
                  Raw
                </TabsTrigger>
              </TabsList>

              {/* Entity Changes Tab */}
              <TabsContent value="changes" className="flex-1">
                <ScrollArea className="h-[400px] pr-4">
                  {details.entityChanges.length === 0 ? (
                    <div className="text-center py-8 text-muted-foreground">
                      <Database className="h-12 w-12 mx-auto mb-4 opacity-50" />
                      <p>{t('activityTimeline.noEntityChanges')}</p>
                    </div>
                  ) : (
                    <div className="space-y-4">
                      {details.entityChanges.map((entityChange) => (
                        <div key={entityChange.id} className="space-y-3">
                          <div className="flex items-center gap-2 text-sm font-medium">
                            <Badge variant="outline">{entityChange.operation}</Badge>
                            <span className="font-mono">{entityChange.entityType}</span>
                            <ArrowRight className="h-4 w-4" />
                            <code className="text-xs bg-muted px-2 py-1 rounded">
                              {entityChange.entityId}
                            </code>
                            <span className="text-muted-foreground ml-auto">
                              v{entityChange.version}
                            </span>
                          </div>
                          <div className="space-y-2 pl-4 border-l-2">
                            {entityChange.changes.map((change, idx) => (
                              <FieldChangeItem key={idx} change={change} />
                            ))}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </ScrollArea>
              </TabsContent>

              {/* DTO Tab */}
              <TabsContent value="dto" className="flex-1">
                <ScrollArea className="h-[400px] pr-4">
                  <div className="space-y-4">
                    {details.dtoDiff ? (
                      <div className="space-y-1.5">
                        <h4 className="text-xs text-muted-foreground font-medium uppercase tracking-wide flex items-center gap-2">
                          <FileJson className="h-3.5 w-3.5" />
                          {t('activityTimeline.handlerChanges')}
                        </h4>
                        <DiffViewer data={details.dtoDiff} />
                      </div>
                    ) : (
                      <div className="text-center py-8 text-muted-foreground">
                        <FileJson className="h-12 w-12 mx-auto mb-4 opacity-50" />
                        <p>{t('activityTimeline.noHandlerDiff')}</p>
                      </div>
                    )}

                    {details.inputParameters && (
                      <div className="space-y-1.5">
                        <h4 className="text-xs text-muted-foreground font-medium uppercase tracking-wide">{t('activityTimeline.inputParameters')}</h4>
                        <JsonViewer data={details.inputParameters} defaultExpanded={true} maxDepth={4} />
                      </div>
                    )}

                    {details.outputResult && (
                      <div className="space-y-1.5">
                        <h4 className="text-xs text-muted-foreground font-medium uppercase tracking-wide">{t('activityTimeline.outputResult')}</h4>
                        <JsonViewer data={details.outputResult} defaultExpanded={false} maxDepth={3} />
                      </div>
                    )}
                  </div>
                </ScrollArea>
              </TabsContent>

              {/* HTTP Tab */}
              <TabsContent value="http" className="flex-1">
                <ScrollArea className="h-[400px] pr-4">
                  {details.httpRequest ? (
                    <div className="space-y-4">
                      {/* Method and Status in a nice row */}
                      <div className="flex items-center gap-6 p-3 bg-muted/50 rounded-lg">
                        <div className="flex items-center gap-3">
                          <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">{t('activityTimeline.method')}</span>
                          <HttpMethodBadge method={details.httpRequest.method} />
                        </div>
                        <div className="h-6 w-px bg-border" />
                        <div className="flex items-center gap-3">
                          <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">{t('activityTimeline.status')}</span>
                          <Badge
                            variant={
                              details.httpRequest.statusCode >= 200 && details.httpRequest.statusCode < 300
                                ? 'default'
                                : details.httpRequest.statusCode >= 400
                                  ? 'destructive'
                                  : 'secondary'
                            }
                            className="font-mono"
                          >
                            {details.httpRequest.statusCode}
                          </Badge>
                        </div>
                        <div className="h-6 w-px bg-border" />
                        <div className="flex items-center gap-3">
                          <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">{t('activityTimeline.duration')}</span>
                          <span className="text-sm font-mono font-medium">
                            {details.httpRequest.durationMs ?? 'N/A'}ms
                          </span>
                        </div>
                      </div>

                      {/* Path */}
                      <div className="space-y-1.5">
                        <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">{t('activityTimeline.path')}</span>
                        <code className="block p-3 bg-muted/50 rounded-lg text-sm font-mono break-all">
                          {details.httpRequest.path}
                          {details.httpRequest.queryString && (
                            <span className="text-muted-foreground">?{details.httpRequest.queryString}</span>
                          )}
                        </code>
                      </div>

                      {/* Client IP */}
                      <div className="space-y-1.5">
                        <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">{t('activityTimeline.clientIp')}</span>
                        <code className="block p-3 bg-muted/50 rounded-lg text-sm font-mono">
                          {details.httpRequest.clientIpAddress || 'N/A'}
                        </code>
                      </div>

                      {/* User Agent */}
                      {details.httpRequest.userAgent && (
                        <div className="space-y-1.5">
                          <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">{t('activityTimeline.userAgent')}</span>
                          <code className="block p-3 bg-muted/50 rounded-lg text-xs font-mono break-all text-muted-foreground">
                            {details.httpRequest.userAgent}
                          </code>
                        </div>
                      )}
                    </div>
                  ) : (
                    <div className="text-center py-8 text-muted-foreground">
                      <Globe className="h-12 w-12 mx-auto mb-4 opacity-50" />
                      <p>{t('activityTimeline.noHttpRequest')}</p>
                    </div>
                  )}
                </ScrollArea>
              </TabsContent>

              {/* Raw Tab */}
              <TabsContent value="raw" className="flex-1">
                <div className="space-y-3">
                  <JsonViewer
                    data={details.entry}
                    defaultExpanded={true}
                    maxDepth={4}
                    title={t('activityTimeline.entryInformation')}
                    maxHeight="340px"
                  />

                  {details.errorMessage && (
                    <div className="space-y-1.5">
                      <h4 className="text-xs font-medium uppercase tracking-wide text-destructive flex items-center gap-2">
                        <AlertCircle className="h-3.5 w-3.5" />
                        {t('activityTimeline.errorMessage')}
                      </h4>
                      <div className="p-3 bg-destructive/10 text-destructive rounded-lg text-sm font-mono break-all">
                        {details.errorMessage}
                      </div>
                    </div>
                  )}
                </div>
              </TabsContent>
            </Tabs>
          ) : null}
        </CredenzaBody>
      </CredenzaContent>
    </Credenza>
  )
}
