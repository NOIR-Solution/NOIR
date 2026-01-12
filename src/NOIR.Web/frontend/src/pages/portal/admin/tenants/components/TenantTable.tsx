import { useTranslation } from 'react-i18next'
import { Edit, Trash2 } from 'lucide-react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Button } from '@/components/ui/button'
import { TenantStatusBadge } from './TenantStatusBadge'
import type { TenantListItem } from '@/types'

interface TenantTableProps {
  tenants: TenantListItem[]
  onEdit: (tenant: TenantListItem) => void
  onDelete: (tenant: TenantListItem) => void
  loading?: boolean
}

export function TenantTable({ tenants, onEdit, onDelete, loading }: TenantTableProps) {
  const { t } = useTranslation('common')

  if (loading) {
    return (
      <div className="flex items-center justify-center py-8">
        <p className="text-muted-foreground">{t('labels.loading')}</p>
      </div>
    )
  }

  if (tenants.length === 0) {
    return (
      <div className="flex items-center justify-center py-8">
        <p className="text-muted-foreground">{t('labels.noData')}</p>
      </div>
    )
  }

  return (
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
              {new Date(tenant.createdAt).toLocaleDateString()}
            </TableCell>
            <TableCell className="text-right">
              <div className="flex items-center justify-end space-x-2">
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => onEdit(tenant)}
                >
                  <Edit className="h-4 w-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => onDelete(tenant)}
                  className="text-destructive hover:text-destructive"
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
