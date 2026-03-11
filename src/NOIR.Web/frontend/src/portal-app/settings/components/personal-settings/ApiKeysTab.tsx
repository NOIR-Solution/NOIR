import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Key,
  Plus,
  RotateCw,
  Ban,
  Copy,
  Check,
  Shield,
  Clock,
  Globe,
  Eye,
  EyeOff,
  AlertTriangle,
} from 'lucide-react'
import { Button } from '@uikit'
import { Badge } from '@uikit'
import { EmptyState } from '@uikit'
import {
  Credenza,
  CredenzaContent,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaDescription,
  CredenzaBody,
  CredenzaFooter,
} from '@uikit'
import { Input } from '@uikit'
import { Label } from '@uikit'
import { Skeleton } from '@uikit'
import { useMyApiKeys, useRotateApiKey, useRevokeApiKey } from '@/hooks/useApiKeys'
import { CreateApiKeyDialog } from './CreateApiKeyDialog'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import type { ApiKeyDto, ApiKeyRotatedDto } from '@/types/apiKey'

export const ApiKeysTab = () => {
  const { t } = useTranslation('common')
  const { data: apiKeys, isLoading } = useMyApiKeys()
  const rotateMutation = useRotateApiKey()
  const revokeMutation = useRevokeApiKey()

  const [createOpen, setCreateOpen] = useState(false)
  const [rotateTarget, setRotateTarget] = useState<ApiKeyDto | null>(null)
  const [revokeTarget, setRevokeTarget] = useState<ApiKeyDto | null>(null)
  const [revokeReason, setRevokeReason] = useState('')
  const [rotatedResult, setRotatedResult] = useState<ApiKeyRotatedDto | null>(null)
  const [copiedField, setCopiedField] = useState<string | null>(null)
  const [showRotatedSecret, setShowRotatedSecret] = useState(false)

  const handleCopy = async (text: string, field: string) => {
    await navigator.clipboard.writeText(text)
    setCopiedField(field)
    toast.success(t('apiKeys.copiedToClipboard'))
    setTimeout(() => setCopiedField(null), 2000)
  }

  const handleRotate = async () => {
    if (!rotateTarget) return
    try {
      const result = await rotateMutation.mutateAsync(rotateTarget.id)
      setRotatedResult(result)
      setRotateTarget(null)
      toast.success(t('apiKeys.rotateSuccess'))
    } catch {
      toast.error(t('apiKeys.rotateError'))
    }
  }

  const handleRevoke = async () => {
    if (!revokeTarget) return
    try {
      await revokeMutation.mutateAsync({
        id: revokeTarget.id,
        request: revokeReason ? { reason: revokeReason } : undefined,
      })
      setRevokeTarget(null)
      setRevokeReason('')
      toast.success(t('apiKeys.revokeSuccess'))
    } catch {
      toast.error(t('apiKeys.revokeError'))
    }
  }

  const getKeyStatus = (key: ApiKeyDto) => {
    if (key.isRevoked) return { label: t('apiKeys.statuses.revoked'), color: 'red' as const }
    if (key.isExpired) return { label: t('apiKeys.statuses.expired'), color: 'gray' as const }
    if (key.isActive) return { label: t('apiKeys.statuses.active'), color: 'green' as const }
    return { label: t('apiKeys.statuses.inactive'), color: 'gray' as const }
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-48" />
          <Skeleton className="h-10 w-36" />
        </div>
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-24 w-full" />
        ))}
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold">{t('apiKeys.title')}</h2>
          <p className="text-sm text-muted-foreground">{t('apiKeys.description')}</p>
        </div>
        <Button onClick={() => setCreateOpen(true)} className="cursor-pointer group transition-all duration-300">
          <Plus className="h-4 w-4 mr-2" />
          {t('apiKeys.create')}
        </Button>
      </div>

      {/* Empty State */}
      {(!apiKeys || apiKeys.length === 0) && (
        <EmptyState
          icon={Key}
          title={t('apiKeys.noKeysFound')}
          description={t('apiKeys.noKeysDescription')}
        />
      )}

      {/* Key List */}
      {apiKeys && apiKeys.length > 0 && (
        <div className="space-y-3">
          {apiKeys.map((apiKey) => {
            const status = getKeyStatus(apiKey)
            return (
              <div
                key={apiKey.id}
                className="rounded-lg border bg-card p-4 shadow-sm hover:shadow-lg transition-all duration-300"
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="min-w-0 flex-1 space-y-2">
                    <div className="flex items-center gap-2">
                      <Key className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                      <span className="font-medium truncate">{apiKey.name}</span>
                      <Badge variant="outline" className={getStatusBadgeClasses(status.color)}>
                        {status.label}
                      </Badge>
                    </div>

                    {apiKey.description && (
                      <p className="text-sm text-muted-foreground">{apiKey.description}</p>
                    )}

                    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
                      <span className="font-mono">{apiKey.keyIdentifier}...{apiKey.secretSuffix}</span>
                      <span className="flex items-center gap-1">
                        <Shield className="h-3 w-3" />
                        {apiKey.permissions.length} {t('apiKeys.permissionsCount')}
                      </span>
                      {apiKey.lastUsedAt ? (
                        <span className="flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          {t('apiKeys.lastUsed')}: {new Date(apiKey.lastUsedAt).toLocaleDateString()}
                        </span>
                      ) : (
                        <span className="flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          {t('apiKeys.neverUsed')}
                        </span>
                      )}
                      {apiKey.lastUsedIp && (
                        <span className="flex items-center gap-1">
                          <Globe className="h-3 w-3" />
                          {apiKey.lastUsedIp}
                        </span>
                      )}
                      {apiKey.expiresAt && (
                        <span>
                          {t('apiKeys.expires')}: {new Date(apiKey.expiresAt).toLocaleDateString()}
                        </span>
                      )}
                    </div>

                    {apiKey.isRevoked && apiKey.revokedReason && (
                      <p className="text-xs text-destructive">
                        {t('apiKeys.revokedReason')}: {apiKey.revokedReason}
                      </p>
                    )}
                  </div>

                  {/* Actions */}
                  {apiKey.isActive && (
                    <div className="flex items-center gap-2 flex-shrink-0">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setRotateTarget(apiKey)}
                        className="cursor-pointer"
                        aria-label={`${t('apiKeys.rotate')} ${apiKey.name}`}
                      >
                        <RotateCw className="h-4 w-4 mr-1" />
                        {t('apiKeys.rotate')}
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setRevokeTarget(apiKey)}
                        className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
                        aria-label={`${t('apiKeys.revoke')} ${apiKey.name}`}
                      >
                        <Ban className="h-4 w-4 mr-1" />
                        {t('apiKeys.revoke')}
                      </Button>
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}

      {/* Create Dialog */}
      <CreateApiKeyDialog open={createOpen} onOpenChange={setCreateOpen} />

      {/* Rotate Confirmation Dialog */}
      <Credenza open={!!rotateTarget} onOpenChange={(open) => !open && setRotateTarget(null)}>
        <CredenzaContent>
          <CredenzaHeader>
            <CredenzaTitle>{t('apiKeys.rotateTitle')}</CredenzaTitle>
            <CredenzaDescription>{t('apiKeys.rotateDescription')}</CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody>
            <div className="rounded-lg border border-amber-500/30 bg-amber-500/5 p-3 flex items-start gap-2">
              <AlertTriangle className="h-5 w-5 text-amber-500 flex-shrink-0 mt-0.5" />
              <p className="text-sm text-amber-700 dark:text-amber-400">{t('apiKeys.rotateWarning')}</p>
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setRotateTarget(null)} className="cursor-pointer">
              {t('buttons.cancel')}
            </Button>
            <Button onClick={handleRotate} disabled={rotateMutation.isPending} className="cursor-pointer">
              {rotateMutation.isPending ? t('apiKeys.rotating') : t('apiKeys.rotate')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Rotated Secret Display Dialog */}
      <Credenza open={!!rotatedResult} onOpenChange={(open) => { if (!open) { setRotatedResult(null); setShowRotatedSecret(false) } }}>
        <CredenzaContent className="sm:max-w-lg">
          <CredenzaHeader>
            <CredenzaTitle>{t('apiKeys.newSecretTitle')}</CredenzaTitle>
            <CredenzaDescription>{t('apiKeys.secretWarning')}</CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody className="space-y-4">
            <div className="rounded-lg border border-amber-500/30 bg-amber-500/5 p-3 flex items-start gap-2">
              <AlertTriangle className="h-5 w-5 text-amber-500 flex-shrink-0 mt-0.5" />
              <p className="text-sm text-amber-700 dark:text-amber-400">{t('apiKeys.secretWarning')}</p>
            </div>
            {rotatedResult && (
              <div className="space-y-2">
                <Label>{t('apiKeys.apiSecret')}</Label>
                <div className="flex items-center gap-2">
                  <code className="flex-1 rounded-md border bg-muted px-3 py-2 text-sm font-mono break-all">
                    {showRotatedSecret ? rotatedResult.secret : '•'.repeat(40)}
                  </code>
                  <Button
                    type="button"
                    variant="outline"
                    size="icon"
                    className="cursor-pointer flex-shrink-0"
                    onClick={() => setShowRotatedSecret(!showRotatedSecret)}
                    aria-label={showRotatedSecret ? t('apiKeys.hideSecret') : t('apiKeys.showSecret')}
                  >
                    {showRotatedSecret ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    size="icon"
                    className="cursor-pointer flex-shrink-0"
                    onClick={() => handleCopy(rotatedResult.secret, 'rotated-secret')}
                    aria-label={t('apiKeys.copySecret')}
                  >
                    {copiedField === 'rotated-secret' ? <Check className="h-4 w-4 text-green-500" /> : <Copy className="h-4 w-4" />}
                  </Button>
                </div>
              </div>
            )}
          </CredenzaBody>
          <CredenzaFooter>
            <Button onClick={() => { setRotatedResult(null); setShowRotatedSecret(false) }} className="cursor-pointer">
              {t('apiKeys.done')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>

      {/* Revoke Confirmation Dialog */}
      <Credenza open={!!revokeTarget} onOpenChange={(open) => { if (!open) { setRevokeTarget(null); setRevokeReason('') } }}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <CredenzaTitle>{t('apiKeys.revokeTitle')}</CredenzaTitle>
            <CredenzaDescription>{t('apiKeys.revokeDescription')}</CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody className="space-y-4">
            <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-3 flex items-start gap-2">
              <AlertTriangle className="h-5 w-5 text-destructive flex-shrink-0 mt-0.5" />
              <p className="text-sm text-destructive">{t('apiKeys.revokeWarning')}</p>
            </div>
            <div className="space-y-2">
              <Label htmlFor="revokeReason">{t('apiKeys.revokeReasonLabel')}</Label>
              <Input
                id="revokeReason"
                placeholder={t('apiKeys.revokeReasonPlaceholder')}
                value={revokeReason}
                onChange={(e) => setRevokeReason(e.target.value)}
              />
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => { setRevokeTarget(null); setRevokeReason('') }} className="cursor-pointer">
              {t('buttons.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleRevoke}
              disabled={revokeMutation.isPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {revokeMutation.isPending ? t('apiKeys.revoking') : t('apiKeys.revoke')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}
