import { useState, useCallback, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Copy, Check, AlertTriangle, Eye, EyeOff } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  Credenza,
  CredenzaContent,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaDescription,
  CredenzaBody,
  CredenzaFooter,
  Button,
  Input,
  Label,
  Textarea,
} from '@uikit'
import { useCreateApiKey } from '@/hooks/useApiKeys'
import { usePermissions } from '@/hooks/usePermissions'
import { PermissionPicker } from '@/components/PermissionPicker'
import type { ApiKeyCreatedDto } from '@/types/apiKey'

const createApiKeySchema = z.object({
  name: z.string().min(1, 'Name is required').max(100),
  description: z.string().max(500).optional(),
  permissions: z.array(z.string()).min(1, 'Select at least one permission'),
  expiresAt: z.string().optional(),
})

type CreateApiKeyFormData = z.infer<typeof createApiKeySchema>

interface CreateApiKeyDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export const CreateApiKeyDialog = ({ open, onOpenChange }: CreateApiKeyDialogProps) => {
  const { t } = useTranslation('common')
  const createMutation = useCreateApiKey()
  const { permissions: userPermissions } = usePermissions()
  const [createdKey, setCreatedKey] = useState<ApiKeyCreatedDto | null>(null)
  const [copiedField, setCopiedField] = useState<'key' | 'secret' | null>(null)
  const [showSecret, setShowSecret] = useState(false)

  // Track selected permissions as a Set for the shared PermissionPicker
  const [selectedPermissionSet, setSelectedPermissionSet] = useState<Set<string>>(new Set())

  const {
    register,
    handleSubmit,
    setValue,
    reset,
    formState: { errors },
  } = useForm<CreateApiKeyFormData>({
    resolver: zodResolver(createApiKeySchema),
    defaultValues: { name: '', description: '', permissions: [], expiresAt: '' },
    mode: 'onBlur',
  })

  // User's own permissions as a Set — only these are shown in the picker (memoized to avoid re-render cascade)
  const allowedPermissions = useMemo(() => new Set(userPermissions), [userPermissions])

  const handlePermissionsChange = useCallback((perms: Set<string>) => {
    setSelectedPermissionSet(perms)
    // Defer form validation to next frame to avoid re-render cascade with Radix Checkbox composeRefs
    requestAnimationFrame(() => {
      setValue('permissions', Array.from(perms), { shouldValidate: true })
    })
  }, [setValue])

  const onSubmit = async (data: CreateApiKeyFormData) => {
    try {
      const result = await createMutation.mutateAsync({
        name: data.name,
        description: data.description || undefined,
        permissions: data.permissions,
        expiresAt: data.expiresAt || undefined,
      })
      setCreatedKey(result)
      toast.success(t('apiKeys.createSuccess'))
    } catch {
      toast.error(t('apiKeys.createError'))
    }
  }

  const handleCopy = async (text: string, field: 'key' | 'secret') => {
    await navigator.clipboard.writeText(text)
    setCopiedField(field)
    toast.success(t('apiKeys.copiedToClipboard'))
    setTimeout(() => setCopiedField(null), 2000)
  }

  const handleClose = (openState: boolean) => {
    if (!openState) {
      setCreatedKey(null)
      setCopiedField(null)
      setShowSecret(false)
      setSelectedPermissionSet(new Set())
      reset()
    }
    onOpenChange(openState)
  }

  // After creation — show secret once
  if (createdKey) {
    return (
      <Credenza open={open} onOpenChange={handleClose}>
        <CredenzaContent className="sm:max-w-lg">
          <CredenzaHeader>
            <CredenzaTitle>{t('apiKeys.keyCreated')}</CredenzaTitle>
            <CredenzaDescription>{t('apiKeys.keyCreatedDescription')}</CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody className="space-y-4">
            <div className="rounded-lg border border-amber-500/30 bg-amber-500/5 p-3 flex items-start gap-2">
              <AlertTriangle className="h-5 w-5 text-amber-500 flex-shrink-0 mt-0.5" />
              <p className="text-sm text-amber-700 dark:text-amber-400">{t('apiKeys.secretWarning')}</p>
            </div>

            <div className="space-y-2">
              <Label>{t('apiKeys.apiKey')}</Label>
              <div className="flex items-center gap-2">
                <code className="flex-1 rounded-md border bg-muted px-3 py-2 text-sm font-mono break-all">
                  {createdKey.keyIdentifier}
                </code>
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  className="cursor-pointer flex-shrink-0"
                  onClick={() => handleCopy(createdKey.keyIdentifier, 'key')}
                  aria-label={t('apiKeys.copyKey')}
                >
                  {copiedField === 'key' ? <Check className="h-4 w-4 text-green-500" /> : <Copy className="h-4 w-4" />}
                </Button>
              </div>
            </div>

            <div className="space-y-2">
              <Label>{t('apiKeys.apiSecret')}</Label>
              <div className="flex items-center gap-2">
                <code className="flex-1 rounded-md border bg-muted px-3 py-2 text-sm font-mono break-all">
                  {showSecret ? createdKey.secret : '•'.repeat(40)}
                </code>
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  className="cursor-pointer flex-shrink-0"
                  onClick={() => setShowSecret(!showSecret)}
                  aria-label={showSecret ? t('apiKeys.hideSecret') : t('apiKeys.showSecret')}
                >
                  {showSecret ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  className="cursor-pointer flex-shrink-0"
                  onClick={() => handleCopy(createdKey.secret, 'secret')}
                  aria-label={t('apiKeys.copySecret')}
                >
                  {copiedField === 'secret' ? <Check className="h-4 w-4 text-green-500" /> : <Copy className="h-4 w-4" />}
                </Button>
              </div>
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button onClick={() => handleClose(false)} className="cursor-pointer">
              {t('apiKeys.done')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    )
  }

  // Create form
  return (
    <Credenza open={open} onOpenChange={handleClose}>
      <CredenzaContent className="sm:max-w-[800px] max-h-[90vh] flex flex-col overflow-hidden">
        <CredenzaHeader>
          <CredenzaTitle>{t('apiKeys.createTitle')}</CredenzaTitle>
          <CredenzaDescription>{t('apiKeys.createDescription')}</CredenzaDescription>
        </CredenzaHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col min-h-0">
          <CredenzaBody className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="name">{t('apiKeys.name')}</Label>
              <Input
                id="name"
                placeholder={t('apiKeys.namePlaceholder')}
                {...register('name')}
              />
              {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">{t('apiKeys.descriptionLabel')}</Label>
              <Textarea
                id="description"
                placeholder={t('apiKeys.descriptionPlaceholder')}
                {...register('description')}
                rows={2}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="expiresAt">{t('apiKeys.expiresAt')}</Label>
              <Input
                id="expiresAt"
                type="datetime-local"
                {...register('expiresAt')}
              />
              <p className="text-xs text-muted-foreground">{t('apiKeys.expiresAtHint')}</p>
            </div>

            <div className="space-y-2">
              <Label>{t('apiKeys.permissions')}</Label>
              {errors.permissions && (
                <p className="text-sm text-destructive">{errors.permissions.message}</p>
              )}
              <PermissionPicker
                selectedPermissions={selectedPermissionSet}
                onPermissionsChange={handlePermissionsChange}
                allowedPermissions={allowedPermissions}
                maxHeight="40vh"
              />
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button type="button" variant="outline" onClick={() => handleClose(false)} className="cursor-pointer">
              {t('buttons.cancel')}
            </Button>
            <Button type="submit" disabled={createMutation.isPending} className="cursor-pointer">
              {createMutation.isPending ? t('apiKeys.creating') : t('apiKeys.create')}
            </Button>
          </CredenzaFooter>
        </form>
      </CredenzaContent>
    </Credenza>
  )
}
