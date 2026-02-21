import { useTranslation } from 'react-i18next'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { Edit, Trash2, Building, KeyRound, Blocks } from 'lucide-react'
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
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
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
    <TooltipProvider delayDuration={300}>
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
                <TableCell className="font-mono text-sm">
                  <button
                    className="hover:underline text-primary cursor-pointer bg-transparent border-none p-0"
                    onClick={() => onEdit(tenant)}
                  >
                    {tenant.identifier}
                  </button>
                </TableCell>
                <TableCell>{tenant.name || '-'}</TableCell>
                <TableCell>
                  <TenantStatusBadge isActive={tenant.isActive} />
                </TableCell>
                <TableCell className="text-sm text-muted-foreground">
                  {formatDate(tenant.createdAt)}
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex items-center justify-end space-x-1">
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="cursor-pointer"
                          onClick={() => onEdit(tenant)}
                          aria-label={t('buttons.edit') + ' ' + (tenant.name || tenant.identifier)}
                        >
                          <Edit className="h-4 w-4" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>{t('buttons.edit')}</TooltipContent>
                    </Tooltip>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="cursor-pointer"
                          onClick={() => onEditModules(tenant)}
                          aria-label={t('tenants.tabs.modules') + ' ' + (tenant.name || tenant.identifier)}
                        >
                          <Blocks className="h-4 w-4" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>{t('tenants.tabs.modules')}</TooltipContent>
                    </Tooltip>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="cursor-pointer"
                          onClick={() => onResetPassword(tenant)}
                          aria-label={t('tenants.resetAdminPassword') + ' ' + (tenant.name || tenant.identifier)}
                        >
                          <KeyRound className="h-4 w-4" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>{t('tenants.resetAdminPassword')}</TooltipContent>
                    </Tooltip>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="cursor-pointer text-destructive hover:text-destructive"
                          onClick={() => onDelete(tenant)}
                          aria-label={t('buttons.delete') + ' ' + (tenant.name || tenant.identifier)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>{t('buttons.delete')}</TooltipContent>
                    </Tooltip>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </TooltipProvider>
  )
}
