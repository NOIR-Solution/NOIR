import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { MoreHorizontal, Edit, Trash2, Shield, Users, Lock, LockOpen, ShieldCheck, Activity } from 'lucide-react'
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
  /** Permission flags to control which actions are shown */
  canEdit?: boolean
  canDelete?: boolean
  canAssignRoles?: boolean
}

export function UserTable({
  users,
  onEdit,
  onDelete,
  onAssignRoles,
  loading,
  canEdit = true,
  canDelete = true,
  canAssignRoles = true,
}: UserTableProps) {
  const { t } = useTranslation('common')
  const navigate = useNavigate()

  const handleViewActivity = (user: UserListItem) => {
    navigate(`/portal/activity-timeline?userId=${encodeURIComponent(user.id)}&userEmail=${encodeURIComponent(user.email)}`)
  }

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
            {(canEdit || canDelete || canAssignRoles) && (
              <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
            )}
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
                  <div className="flex items-center gap-2">
                    <p className="font-medium">
                      {user.displayName || user.email.split('@')[0]}
                    </p>
                    {user.isSystemUser && (
                      <Badge variant="secondary" className="gap-1 text-xs">
                        <ShieldCheck className="h-3 w-3" />
                        {t('users.systemUser', 'System')}
                      </Badge>
                    )}
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
              {(canEdit || canDelete || canAssignRoles) && (
                <TableCell className="text-right">
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="sm">
                        <MoreHorizontal className="h-4 w-4" />
                        <span className="sr-only">{t('labels.openMenu', 'Open menu')}</span>
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      {canAssignRoles && (
                        <DropdownMenuItem onClick={() => onAssignRoles(user)}>
                          <Shield className="mr-2 h-4 w-4" />
                          {t('users.assignRoles', 'Assign Roles')}
                        </DropdownMenuItem>
                      )}
                      {canEdit && (
                        <DropdownMenuItem onClick={() => onEdit(user)}>
                          <Edit className="mr-2 h-4 w-4" />
                          {t('buttons.edit', 'Edit')}
                        </DropdownMenuItem>
                      )}
                      <DropdownMenuItem onClick={() => handleViewActivity(user)}>
                        <Activity className="mr-2 h-4 w-4" />
                        {t('users.viewActivity', 'View Activity')}
                      </DropdownMenuItem>
                      {canDelete && (canAssignRoles || canEdit) && <DropdownMenuSeparator />}
                      {canDelete && (
                        user.isSystemUser ? (
                          <DropdownMenuItem disabled className="text-muted-foreground">
                            <ShieldCheck className="mr-2 h-4 w-4" />
                            {t('users.protectedSystemUser', 'Protected (System User)')}
                          </DropdownMenuItem>
                        ) : (
                          <DropdownMenuItem
                            onClick={() => onDelete(user)}
                            className="text-destructive focus:text-destructive"
                          >
                            <Trash2 className="mr-2 h-4 w-4" />
                            {user.isLocked ? t('users.unlock', 'Unlock') : t('users.lock', 'Lock')}
                          </DropdownMenuItem>
                        )
                      )}
                    </DropdownMenuContent>
                  </DropdownMenu>
                </TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}
