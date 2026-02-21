import { useTranslation } from 'react-i18next'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { Edit, Trash2, Building, KeyRound, Blocks, MoreHorizontal } from 'lucide-react'
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
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
  onEditModules: (tenant: TenantListItem) => void
  onDelete: (tenant: TenantListItem) => void
  onResetPassword: (tenant: TenantListItem) => void
  loading?: boolean
}

export const TenantTable = ({ tenants, onEdit, onEditModules, onDelete, onResetPassword, loading }: TenantTableProps) => {
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
            <TableHead className="hidden sm:table-cell">{t('tenants.table.identifier')}</TableHead>
            <TableHead>{t('tenants.table.name')}</TableHead>
            <TableHead>{t('labels.status')}</TableHead>
            <TableHead className="hidden md:table-cell">{t('labels.createdAt')}</TableHead>
            <TableHead className="text-right">{t('labels.actions')}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {tenants.map((tenant) => (
            <TableRow key={tenant.id}>
              <TableCell className="hidden sm:table-cell font-mono text-sm">
                <button
                  className="hover:underline text-primary cursor-pointer bg-transparent border-none p-0"
                  onClick={() => onEdit(tenant)}
                >
                  {tenant.identifier}
                </button>
              </TableCell>
              <TableCell>
                <button
                  className="hover:underline text-left cursor-pointer bg-transparent border-none p-0 sm:cursor-default sm:no-underline"
                  onClick={() => onEdit(tenant)}
                >
                  <span className="font-medium">{tenant.name || tenant.identifier}</span>
                  <span className="block text-xs text-muted-foreground sm:hidden">{tenant.identifier}</span>
                </button>
              </TableCell>
              <TableCell>
                <TenantStatusBadge isActive={tenant.isActive} />
              </TableCell>
              <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                {formatDate(tenant.createdAt)}
              </TableCell>
              <TableCell className="text-right">
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="cursor-pointer"
                      aria-label={t('labels.actionsFor', { name: tenant.name || tenant.identifier, defaultValue: `Actions for ${tenant.name || tenant.identifier}` })}
                    >
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => onEdit(tenant)} className="cursor-pointer">
                      <Edit className="mr-2 h-4 w-4" />
                      {t('buttons.edit')}
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => onEditModules(tenant)} className="cursor-pointer">
                      <Blocks className="mr-2 h-4 w-4" />
                      {t('tenants.tabs.modules')}
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => onResetPassword(tenant)} className="cursor-pointer">
                      <KeyRound className="mr-2 h-4 w-4" />
                      {t('tenants.resetAdminPassword')}
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem
                      onClick={() => onDelete(tenant)}
                      className="text-destructive focus:text-destructive cursor-pointer"
                    >
                      <Trash2 className="mr-2 h-4 w-4" />
                      {t('buttons.delete')}
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}
