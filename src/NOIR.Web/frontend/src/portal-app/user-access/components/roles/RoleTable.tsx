import { useTranslation } from 'react-i18next'
import { EllipsisVertical, Edit, Trash2, Key, Shield, Users } from 'lucide-react'
import {
  Badge,
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

import type { RoleListItem } from '@/types'

interface RoleTableProps {
  roles: RoleListItem[]
  onEdit: (role: RoleListItem) => void
  onDelete: (role: RoleListItem) => void
  onPermissions: (role: RoleListItem) => void
  loading?: boolean
}

export const RoleTable = ({ roles, onEdit, onDelete, onPermissions, loading }: RoleTableProps) => {
  const { t } = useTranslation('common')

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

  if (roles.length === 0) {
    return (
      <EmptyState
        icon={Shield}
        title={t('roles.noRoles', 'No roles found')}
        description={t('roles.noRolesDescription', 'Create a new role to get started.')}
      />
    )
  }

  return (
    <div className="rounded-xl border border-border/50 overflow-hidden">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="w-10 sticky left-0 z-10 bg-background" />
            <TableHead>{t('roles.columns.name', 'Name')}</TableHead>
            <TableHead>{t('roles.columns.description', 'Description')}</TableHead>
            <TableHead className="text-center">{t('roles.columns.users', 'Users')}</TableHead>
            <TableHead className="text-center">{t('roles.columns.type', 'Type')}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {roles.map((role) => (
            <TableRow key={role.id}>
              <TableCell className="sticky left-0 z-10 bg-background">
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="cursor-pointer"
                      aria-label={t('labels.actionsFor', { name: role.name, defaultValue: `Actions for ${role.name}` })}
                    >
                      <EllipsisVertical className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="start">
                    <DropdownMenuItem onClick={() => onPermissions(role)}>
                      <Key className="mr-2 h-4 w-4" />
                      {t('roles.managePermissions', 'Manage Permissions')}
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => onEdit(role)}>
                      <Edit className="mr-2 h-4 w-4" />
                      {t('buttons.edit', 'Edit')}
                    </DropdownMenuItem>
                    {!role.isSystemRole && (
                      <>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem
                          onClick={() => onDelete(role)}
                          className="text-destructive focus:text-destructive"
                        >
                          <Trash2 className="mr-2 h-4 w-4" />
                          {t('buttons.delete', 'Delete')}
                        </DropdownMenuItem>
                      </>
                    )}
                  </DropdownMenuContent>
                </DropdownMenu>
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-3">
                  <div
                    className="w-8 h-8 rounded-full flex items-center justify-center"
                    style={{ backgroundColor: role.color || '#6b7280' }}
                  >
                    <Shield className="h-4 w-4 text-white" />
                  </div>
                  <div>
                    <p className="font-medium">{role.name}</p>
                    {role.parentRoleId && (
                      <p className="text-xs text-muted-foreground">
                        {t('roles.inheritsFrom', 'Inherits permissions')}
                      </p>
                    )}
                  </div>
                </div>
              </TableCell>
              <TableCell>
                <span className="text-muted-foreground line-clamp-2">
                  {role.description || '-'}
                </span>
              </TableCell>
              <TableCell className="text-center">
                <div className="flex items-center justify-center gap-1">
                  <Users className="h-4 w-4 text-muted-foreground" />
                  <span>{role.userCount}</span>
                </div>
              </TableCell>
              <TableCell className="text-center">
                {role.isSystemRole ? (
                  <Badge variant="secondary">{t('roles.system', 'System')}</Badge>
                ) : (
                  <Badge variant="outline">{t('roles.custom', 'Custom')}</Badge>
                )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}
