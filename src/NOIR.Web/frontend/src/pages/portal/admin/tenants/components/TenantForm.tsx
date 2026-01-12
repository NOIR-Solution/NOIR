import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { Tenant, CreateTenantRequest, UpdateTenantRequest } from '@/types'

interface TenantFormBaseProps {
  onCancel: () => void
  loading?: boolean
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

export function TenantForm({ tenant, onSubmit, onCancel, loading }: TenantFormProps) {
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

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="identifier">{t('tenants.form.identifier')}</Label>
        <Input
          id="identifier"
          value={formData.identifier}
          onChange={(e) => setFormData(prev => ({ ...prev, identifier: e.target.value }))}
          disabled={loading}
          placeholder={t('tenants.form.identifierPlaceholder')}
          required
        />
        <p className="text-xs text-muted-foreground">{t('tenants.form.identifierHint')}</p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="name">{t('tenants.form.name')}</Label>
        <Input
          id="name"
          value={formData.name}
          onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
          disabled={loading}
          placeholder={t('tenants.form.namePlaceholder')}
          required
        />
      </div>

      {isEditing && (
        <div className="flex items-center space-x-2">
          <input
            type="checkbox"
            id="isActive"
            checked={formData.isActive}
            onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
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
