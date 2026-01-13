import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { Tenant, CreateTenantRequest, UpdateTenantRequest } from '@/types'

interface TenantFormBaseProps {
  onCancel: () => void
  loading?: boolean
  /** Field-level validation errors from the server (keyed by field name) */
  errors?: Record<string, string[]>
  /** Callback when user modifies a field (to clear that field's error) */
  onFieldChange?: (field: string) => void
}

interface CreateFormProps extends TenantFormBaseProps {
  tenant?: null
  onSubmit: (data: CreateTenantRequest) => Promise<void>
}

interface EditFormProps extends TenantFormBaseProps {
  tenant: Tenant
  onSubmit: (data: UpdateTenantRequest) => Promise<void>
}

type TenantFormProps = CreateFormProps | EditFormProps

/**
 * Get error messages for a field (handles both PascalCase and camelCase keys)
 */
function getFieldErrors(errors: Record<string, string[]> | undefined, fieldName: string): string[] {
  if (!errors) return []
  // Try both PascalCase (from C#) and camelCase
  const pascalCase = fieldName.charAt(0).toUpperCase() + fieldName.slice(1)
  return errors[pascalCase] || errors[fieldName] || []
}

export function TenantForm({ tenant, onSubmit, onCancel, loading, errors, onFieldChange }: TenantFormProps) {
  const { t } = useTranslation('common')
  const isEditing = !!tenant

  const [formData, setFormData] = useState({
    identifier: '',
    name: '',
    isActive: true,
  })

  useEffect(() => {
    if (tenant) {
      setFormData({
        identifier: tenant.identifier,
        name: tenant.name || '',
        isActive: tenant.isActive,
      })
    }
  }, [tenant])

  const handleFieldChange = (field: keyof typeof formData, value: string | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }))
    // Notify parent to clear this field's error
    onFieldChange?.(field)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (tenant) {
      const updateData: UpdateTenantRequest = {
        identifier: formData.identifier,
        name: formData.name,
        isActive: formData.isActive,
      }
      // Type assertion needed because TypeScript can't narrow discriminated unions from variables
      await (onSubmit as (data: UpdateTenantRequest) => Promise<void>)(updateData)
    } else {
      const createData: CreateTenantRequest = {
        identifier: formData.identifier,
        name: formData.name,
      }
      await (onSubmit as (data: CreateTenantRequest) => Promise<void>)(createData)
    }
  }

  const identifierErrors = getFieldErrors(errors, 'identifier')
  const nameErrors = getFieldErrors(errors, 'name')

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="identifier">{t('tenants.form.identifier')}</Label>
        <Input
          id="identifier"
          value={formData.identifier}
          onChange={(e) => handleFieldChange('identifier', e.target.value)}
          disabled={loading}
          placeholder={t('tenants.form.identifierPlaceholder')}
          required
          aria-invalid={identifierErrors.length > 0}
          className={identifierErrors.length > 0 ? 'border-destructive focus-visible:ring-destructive' : ''}
        />
        {identifierErrors.length > 0 ? (
          <p className="text-xs text-destructive">{identifierErrors.join('. ')}</p>
        ) : (
          <p className="text-xs text-muted-foreground">{t('tenants.form.identifierHint')}</p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="name">{t('tenants.form.name')}</Label>
        <Input
          id="name"
          value={formData.name}
          onChange={(e) => handleFieldChange('name', e.target.value)}
          disabled={loading}
          placeholder={t('tenants.form.namePlaceholder')}
          required
          aria-invalid={nameErrors.length > 0}
          className={nameErrors.length > 0 ? 'border-destructive focus-visible:ring-destructive' : ''}
        />
        {nameErrors.length > 0 && (
          <p className="text-xs text-destructive">{nameErrors.join('. ')}</p>
        )}
      </div>

      {isEditing && (
        <div className="flex items-center space-x-2">
          <input
            type="checkbox"
            id="isActive"
            checked={formData.isActive}
            onChange={(e) => handleFieldChange('isActive', e.target.checked)}
            disabled={loading}
            className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
          />
          <Label htmlFor="isActive">{t('labels.active')}</Label>
        </div>
      )}

      <div className="flex justify-end space-x-2 pt-4">
        <Button type="button" variant="outline" onClick={onCancel} disabled={loading}>
          {t('buttons.cancel')}
        </Button>
        <Button type="submit" disabled={loading}>
          {loading ? t('labels.loading') : isEditing ? t('buttons.update') : t('buttons.create')}
        </Button>
      </div>
    </form>
  )
}
