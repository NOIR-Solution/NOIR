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
    logoUrl: '',
    primaryColor: '',
    accentColor: '',
    theme: '',
    isActive: true,
  })

  useEffect(() => {
    if (tenant) {
      setFormData({
        identifier: tenant.identifier,
        name: tenant.name || '',
        logoUrl: tenant.logoUrl || '',
        primaryColor: tenant.primaryColor || '',
        accentColor: tenant.accentColor || '',
        theme: tenant.theme || '',
        isActive: tenant.isActive,
      })
    }
  }, [tenant])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (tenant) {
      const updateData: UpdateTenantRequest = {
        name: formData.name,
        logoUrl: formData.logoUrl || null,
        primaryColor: formData.primaryColor || null,
        accentColor: formData.accentColor || null,
        theme: formData.theme || null,
        isActive: formData.isActive,
      }
      // Type assertion needed because TypeScript can't narrow discriminated unions from variables
      await (onSubmit as (data: UpdateTenantRequest) => Promise<void>)(updateData)
    } else {
      const createData: CreateTenantRequest = {
        identifier: formData.identifier,
        name: formData.name,
        logoUrl: formData.logoUrl || null,
        primaryColor: formData.primaryColor || null,
        accentColor: formData.accentColor || null,
        theme: formData.theme || null,
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
          disabled={isEditing || loading}
          placeholder={t('tenants.form.identifierPlaceholder')}
          required
        />
        {!isEditing && (
          <p className="text-xs text-muted-foreground">{t('tenants.form.identifierHint')}</p>
        )}
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

      <div className="space-y-2">
        <Label htmlFor="logoUrl">{t('tenants.form.logoUrl')}</Label>
        <Input
          id="logoUrl"
          type="url"
          value={formData.logoUrl}
          onChange={(e) => setFormData(prev => ({ ...prev, logoUrl: e.target.value }))}
          disabled={loading}
          placeholder="https://example.com/logo.png"
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label htmlFor="primaryColor">{t('tenants.form.primaryColor')}</Label>
          <Input
            id="primaryColor"
            value={formData.primaryColor}
            onChange={(e) => setFormData(prev => ({ ...prev, primaryColor: e.target.value }))}
            disabled={loading}
            placeholder="#3B82F6"
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="accentColor">{t('tenants.form.accentColor')}</Label>
          <Input
            id="accentColor"
            value={formData.accentColor}
            onChange={(e) => setFormData(prev => ({ ...prev, accentColor: e.target.value }))}
            disabled={loading}
            placeholder="#10B981"
          />
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="theme">{t('tenants.form.theme')}</Label>
        <Input
          id="theme"
          value={formData.theme}
          onChange={(e) => setFormData(prev => ({ ...prev, theme: e.target.value }))}
          disabled={loading}
          placeholder="light"
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
