import { useTranslation } from 'react-i18next'
import { MoreHorizontal, Edit, Trash2, Shield, Users, Lock, LockOpen } from 'lucide-react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/ui/empty-state'
import type { UserListItem } from '@/types'

interface UserTableProps {
  users: UserListItem[]
  onEdit: (user: UserListItem) => void
  onDelete: (user: UserListItem) => void
  onAssignRoles: (user: UserListItem) => void
  loading?: boolean
}

export function UserTable({ users, onEdit, onDelete, onAssignRoles, loading }: UserTableProps) {
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

  if (users.length === 0) {
    return (
      <EmptyState
        icon={Users}
        title={t('users.noUsers', 'No users found')}
        description={t('users.noUsersDescription', 'No users match your current filters.')}
      />
    )
  }

  const getInitials = (user: UserListItem) => {
    if (user.displayName) {
      return user.displayName.charAt(0).toUpperCase()
    }
    return user.email.charAt(0).toUpperCase()
  }

  return (
    <div className="rounded-md border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>{t('users.columns.user', 'User')}</TableHead>
            <TableHead>{t('users.columns.email', 'Email')}</TableHead>
            <TableHead>{t('users.columns.roles', 'Roles')}</TableHead>
            <TableHead className="text-center">{t('users.columns.status', 'Status')}</TableHead>
            <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map((user) => (
            <TableRow key={user.id}>
              <TableCell>
                <div className="flex items-center gap-3">
                  <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
                    <span className="text-primary text-sm font-medium">
                      {getInitials(user)}
                    </span>
                  </div>
                  <div>
                    <p className="font-medium">
                      {user.displayName || user.email.split('@')[0]}
                    </p>
                  </div>
                </div>
              </TableCell>
              <TableCell>
                <span className="text-muted-foreground">{user.email}</span>
              </TableCell>
              <TableCell>
                <div className="flex flex-wrap gap-1">
                  {user.roles.length > 0 ? (
                    user.roles.slice(0, 3).map((role) => (
                      <Badge key={role} variant="outline" className="text-xs">
                        {role}
                      </Badge>
                    ))
                  ) : (
                    <span className="text-muted-foreground text-sm">-</span>
                  )}
                  {user.roles.length > 3 && (
                    <Badge variant="secondary" className="text-xs">
                      +{user.roles.length - 3}
                    </Badge>
                  )}
                </div>
              </TableCell>
              <TableCell className="text-center">
                {user.isLocked ? (
                  <Badge variant="destructive" className="gap-1">
                    <Lock className="h-3 w-3" />
                    {t('users.locked', 'Locked')}
                  </Badge>
                ) : (
                  <Badge variant="default" className="gap-1 bg-green-600">
                    <LockOpen className="h-3 w-3" />
                    {t('labels.active', 'Active')}
                  </Badge>
                )}
              </TableCell>
              <TableCell className="text-right">
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="sm">
                      <MoreHorizontal className="h-4 w-4" />
                      <span className="sr-only">{t('labels.openMenu', 'Open menu')}</span>
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => onAssignRoles(user)}>
                      <Shield className="mr-2 h-4 w-4" />
                      {t('users.assignRoles', 'Assign Roles')}
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => onEdit(user)}>
                      <Edit className="mr-2 h-4 w-4" />
                      {t('buttons.edit', 'Edit')}
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem
                      onClick={() => onDelete(user)}
                      className="text-destructive focus:text-destructive"
                    >
                      <Trash2 className="mr-2 h-4 w-4" />
                      {user.isLocked ? t('users.unlock', 'Unlock') : t('users.lock', 'Lock')}
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
