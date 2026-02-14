import { useTranslation } from 'react-i18next'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { Edit, Trash2, Building, KeyRound } from 'lucide-react'
import {
  Button,
  EmptyState,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'

import { TenantStatusBadge } from './TenantStatusBadge'
import type { TenantListItem } from '@/types'

interface TenantTableProps {
  tenants: TenantListItem[]
  onEdit: (tenant: TenantListItem) => void
  onDelete: (tenant: TenantListItem) => void
  onResetPassword: (tenant: TenantListItem) => void
  loading?: boolean
}

export function TenantTable({ tenants, onEdit, onDelete, onResetPassword, loading }: TenantTableProps) {
  const { t } = useTranslation('common')
  const { formatDate } = useRegionalSettings()

  if (loading) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="flex items-center space-x-4">
            <Skeleton className="h-10 w-10 rounded-full" />
            <div className="space-y-2">
              <Skeleton className="h-4 w-[200px]" />
              <Skeleton className="h-3 w-[150px]" />
            </div>
          </div>
        ))}
      </div>
    )
  }

  if (tenants.length === 0) {
    return (
      <EmptyState
        icon={Building}
        title={t('tenants.noTenants', 'No tenants found')}
        description={t('tenants.noTenantsDescription', 'Create a new tenant to get started.')}
      />
    )
  }

  return (
    <div className="rounded-xl border border-border/50 overflow-hidden">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>{t('tenants.table.identifier')}</TableHead>
            <TableHead>{t('tenants.table.name')}</TableHead>
            <TableHead>{t('labels.status')}</TableHead>
            <TableHead>{t('labels.createdAt')}</TableHead>
            <TableHead className="text-right">{t('labels.actions')}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {tenants.map((tenant) => (
            <TableRow key={tenant.id}>
              <TableCell className="font-mono text-sm">{tenant.identifier}</TableCell>
              <TableCell>{tenant.name || '-'}</TableCell>
              <TableCell>
                <TenantStatusBadge isActive={tenant.isActive} />
              </TableCell>
              <TableCell className="text-sm text-muted-foreground">
                {formatDate(tenant.createdAt)}
              </TableCell>
              <TableCell className="text-right">
                <div className="flex items-center justify-end space-x-2">
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => onEdit(tenant)}
                    title={t('buttons.edit')}
                  >
                    <Edit className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => onResetPassword(tenant)}
                    title={t('tenants.resetAdminPassword')}
                  >
                    <KeyRound className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => onDelete(tenant)}
                    className="text-destructive hover:text-destructive"
                    title={t('buttons.delete')}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}
