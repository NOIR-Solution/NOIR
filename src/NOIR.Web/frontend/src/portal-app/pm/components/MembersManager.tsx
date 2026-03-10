import { useState, useDeferredValue, useRef, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { UserPlus, Loader2, Crown, Shield, Eye, Users, Search, X, Check } from 'lucide-react'
import {
  Button,
  Avatar,
  Credenza,
  CredenzaContent,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaDescription,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Input,
} from '@uikit'
import { useAddMember, useRemoveMember, useChangeMemberRole } from '@/portal-app/pm/queries'
import { useEmployeeSearchQuery } from '@/portal-app/hr/queries'
import type { ProjectMemberDto, ProjectMemberRole } from '@/types/pm'
import type { EmployeeSearchDto } from '@/types/hr'

const roleIconMap: Record<ProjectMemberRole, React.ElementType> = {
  Owner: Crown,
  Manager: Shield,
  Member: Users,
  Viewer: Eye,
}

const roleColorClass: Record<ProjectMemberRole, string> = {
  Owner: 'text-amber-500',
  Manager: 'text-blue-500',
  Member: 'text-green-500',
  Viewer: 'text-muted-foreground',
}

interface MembersManagerProps {
  projectId: string
  members: ProjectMemberDto[]
}

export const MembersManager = ({ projectId, members }: MembersManagerProps) => {
  const { t } = useTranslation('common')
  const addMemberMutation = useAddMember()
  const removeMemberMutation = useRemoveMember()
  const changeMemberRoleMutation = useChangeMemberRole()

  const [searchInput, setSearchInput] = useState('')
  const [dropdownOpen, setDropdownOpen] = useState(false)
  const [selectedEmployee, setSelectedEmployee] = useState<EmployeeSearchDto | null>(null)
  const [newRole, setNewRole] = useState<ProjectMemberRole>('Member')
  const [confirmRemoveId, setConfirmRemoveId] = useState<string | null>(null)
  const searchContainerRef = useRef<HTMLDivElement>(null)

  const deferredQuery = useDeferredValue(searchInput)
  const { data: searchResults, isFetching: isSearching } = useEmployeeSearchQuery(deferredQuery)

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (searchContainerRef.current && !searchContainerRef.current.contains(e.target as Node)) {
        setDropdownOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const confirmRemoveMember = members.find(m => m.id === confirmRemoveId)

  // Filter out already-added members
  const filteredResults = (searchResults ?? []).filter(
    (emp) => !members.some((m) => m.employeeId === emp.id)
  )

  const handleSelectEmployee = (emp: EmployeeSearchDto) => {
    setSelectedEmployee(emp)
    setSearchInput(emp.fullName)
    setDropdownOpen(false)
  }

  const handleClearSelection = () => {
    setSelectedEmployee(null)
    setSearchInput('')
    setDropdownOpen(false)
  }

  const handleAddMember = () => {
    if (!selectedEmployee) return
    addMemberMutation.mutate(
      { projectId, request: { employeeId: selectedEmployee.id, role: newRole } },
      {
        onSuccess: () => {
          toast.success(t('pm.addMember'))
          setSelectedEmployee(null)
          setSearchInput('')
          setDropdownOpen(false)
          setNewRole('Member')
        },
        onError: (err) => {
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
        },
      },
    )
  }

  const handleRemoveMember = (memberId: string) => {
    removeMemberMutation.mutate(
      { projectId, memberId },
      {
        onSuccess: () => {
          toast.success(t('pm.removeMember'))
          setConfirmRemoveId(null)
        },
        onError: (err) => {
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
        },
      },
    )
  }

  const handleRoleChange = (memberId: string, role: ProjectMemberRole) => {
    changeMemberRoleMutation.mutate(
      { projectId, memberId, role },
      {
        onError: (err) => {
          toast.error(err instanceof Error ? err.message : t('errors.unknown'))
        },
      },
    )
  }

  return (
    <div className="space-y-4">
      {/* Add member row */}
      <div className="flex gap-2">
        {/* Employee search — inline combobox, no popover portal */}
        <div ref={searchContainerRef} className="relative flex-1 min-w-0">
          <div className="flex items-center gap-1.5 h-9 rounded-md border border-input bg-background px-2.5 text-sm transition-colors focus-within:ring-1 focus-within:ring-ring focus-within:border-ring">
            {isSearching
              ? <Loader2 className="h-3.5 w-3.5 text-muted-foreground animate-spin flex-shrink-0" />
              : <Search className="h-3.5 w-3.5 text-muted-foreground flex-shrink-0" />
            }
            {selectedEmployee ? (
              <>
                <div className="h-5 w-5 rounded-full bg-primary/10 flex items-center justify-center text-[10px] font-semibold text-primary flex-shrink-0">
                  {selectedEmployee.fullName.charAt(0).toUpperCase()}
                </div>
                <span className="flex-1 truncate text-sm">{selectedEmployee.fullName}</span>
                <span className="text-xs text-muted-foreground flex-shrink-0 font-mono">{selectedEmployee.employeeCode}</span>
                <button
                  type="button"
                  onClick={handleClearSelection}
                  className="ml-1 h-5 w-5 rounded-full flex items-center justify-center text-muted-foreground hover:text-foreground hover:bg-muted transition-colors flex-shrink-0 cursor-pointer"
                  aria-label={t('buttons.clear', { defaultValue: 'Clear' })}
                >
                  <X className="h-3 w-3" />
                </button>
              </>
            ) : (
              <Input
                value={searchInput}
                onChange={(e) => {
                  setSearchInput(e.target.value)
                  setSelectedEmployee(null)
                  setDropdownOpen(true)
                }}
                onFocus={() => setDropdownOpen(true)}
                placeholder={t('pm.searchMemberPlaceholder', { defaultValue: 'Search by name, email or code…' })}
                className="flex-1 h-auto border-0 bg-transparent px-0 py-0 text-sm focus-visible:ring-0 focus-visible:ring-offset-0 placeholder:text-muted-foreground/60"
              />
            )}
          </div>

          {/* Inline dropdown — no portal, stays within dialog flow */}
          {dropdownOpen && !selectedEmployee && (
            <div className="absolute left-0 right-0 top-full mt-1 z-50 rounded-md border border-border bg-popover shadow-md overflow-hidden">
              <div className="max-h-52 overflow-y-auto" style={{ scrollbarWidth: 'thin' }}>
                {deferredQuery.length < 2 ? (
                  <p className="text-xs text-muted-foreground text-center py-4 px-3">
                    {t('pm.typeToSearch', { defaultValue: 'Type at least 2 characters to search…' })}
                  </p>
                ) : filteredResults.length === 0 && !isSearching ? (
                  <p className="text-xs text-muted-foreground text-center py-4 px-3">
                    {t('pm.noEmployeesFound', { defaultValue: 'No employees found' })}
                  </p>
                ) : (
                  <div className="p-1">
                    {filteredResults.map((emp) => (
                      <EmployeeResultRow
                        key={emp.id}
                        emp={emp}
                        isSelected={false}
                        onSelect={handleSelectEmployee}
                      />
                    ))}
                  </div>
                )}
              </div>
            </div>
          )}
        </div>

        {/* Role selector */}
        <Select value={newRole} onValueChange={(v) => setNewRole(v as ProjectMemberRole)}>
          <SelectTrigger className="w-28 h-9 text-xs cursor-pointer flex-shrink-0">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Manager" className="cursor-pointer text-xs">{t('memberRoles.manager')}</SelectItem>
            <SelectItem value="Member" className="cursor-pointer text-xs">{t('memberRoles.member')}</SelectItem>
            <SelectItem value="Viewer" className="cursor-pointer text-xs">{t('memberRoles.viewer')}</SelectItem>
          </SelectContent>
        </Select>

        {/* Add button */}
        <Button
          size="sm"
          className="cursor-pointer h-9 px-3 gap-1.5 flex-shrink-0"
          onClick={handleAddMember}
          disabled={!selectedEmployee || addMemberMutation.isPending}
          aria-label={t('pm.addMember')}
        >
          {addMemberMutation.isPending
            ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
            : <UserPlus className="h-3.5 w-3.5" />
          }
          <span className="hidden sm:inline">{t('pm.addMember')}</span>
        </Button>
      </div>

      {/* Trello-style section label */}
      <div className="flex items-center gap-2 pt-1 border-t border-border/40">
        <span className="text-sm font-semibold text-primary">
          {t('pm.projectMembers', { defaultValue: 'Project members' })}
        </span>
        <span className="text-xs bg-muted text-muted-foreground rounded px-1.5 py-0.5 font-medium leading-[1.1]">
          {members.length}
        </span>
      </div>

      {/* Member list */}
      <div className="space-y-0.5">
        {members.length === 0 && (
          <p className="text-sm text-muted-foreground text-center py-4">
            {t('pm.noMembers', { defaultValue: 'No members yet' })}
          </p>
        )}
        {members.map((member) => {
          const RoleIcon = roleIconMap[member.role]
          return (
            <div
              key={member.id}
              className="flex items-center gap-3 px-2 py-2 rounded-lg hover:bg-muted/40 transition-colors group"
            >
              {/* Avatar */}
              <Avatar
                src={member.avatarUrl ?? undefined}
                alt={member.employeeName}
                fallback={member.employeeName}
                size="sm"
                className="h-8 w-8 flex-shrink-0 text-xs"
              />

              {/* Name + subtitle (Trello pattern: name bold, code + role muted below) */}
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-1.5">
                  <span className="text-sm font-medium truncate">{member.employeeName}</span>
                  <RoleIcon className={`h-3 w-3 flex-shrink-0 ${roleColorClass[member.role]}`} />
                </div>
              </div>

              {/* Role selector or owner label */}
              {member.role === 'Owner' ? (
                <span className="text-xs text-amber-500 font-medium flex-shrink-0">
                  {t('memberRoles.owner', { defaultValue: 'Owner' })}
                </span>
              ) : (
                <div className="flex items-center gap-1 flex-shrink-0">
                  <Select
                    value={member.role}
                    onValueChange={(value) => handleRoleChange(member.id, value as ProjectMemberRole)}
                  >
                    <SelectTrigger
                      className="w-24 h-7 text-xs cursor-pointer border-transparent bg-transparent hover:bg-muted hover:border-border transition-colors"
                      aria-label={`${t('pm.memberRole')} - ${member.employeeName}`}
                    >
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Manager" className="cursor-pointer text-xs">{t('memberRoles.manager')}</SelectItem>
                      <SelectItem value="Member" className="cursor-pointer text-xs">{t('memberRoles.member')}</SelectItem>
                      <SelectItem value="Viewer" className="cursor-pointer text-xs">{t('memberRoles.viewer')}</SelectItem>
                    </SelectContent>
                  </Select>
                  <button
                    className="opacity-0 group-hover:opacity-100 h-6 w-6 rounded flex items-center justify-center text-muted-foreground hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-950/30 transition-all cursor-pointer flex-shrink-0"
                    onClick={() => setConfirmRemoveId(member.id)}
                    aria-label={`${t('pm.removeMember')} ${member.employeeName}`}
                  >
                    <X className="h-3 w-3" />
                  </button>
                </div>
              )}
            </div>
          )
        })}
      </div>

      {/* Remove confirmation dialog */}
      <Credenza open={!!confirmRemoveId} onOpenChange={(open) => !open && setConfirmRemoveId(null)}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <CredenzaTitle>{t('pm.removeMember')}</CredenzaTitle>
            <CredenzaDescription>
              {confirmRemoveMember?.employeeName}
            </CredenzaDescription>
          </CredenzaHeader>
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setConfirmRemoveId(null)} className="cursor-pointer">
              {t('buttons.cancel')}
            </Button>
            <Button
              variant="destructive"
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
              onClick={() => confirmRemoveId && handleRemoveMember(confirmRemoveId)}
              disabled={removeMemberMutation.isPending}
            >
              {removeMemberMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t('pm.removeMember')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

// ── Employee result row ──────────────────────────────────────────────────────

interface EmployeeResultRowProps {
  emp: EmployeeSearchDto
  isSelected: boolean
  onSelect: (emp: EmployeeSearchDto) => void
}

const EmployeeResultRow = ({ emp, isSelected, onSelect }: EmployeeResultRowProps) => (
  <button
    type="button"
    onClick={() => onSelect(emp)}
    className="w-full flex items-center gap-2.5 px-2 py-1.5 rounded-md hover:bg-accent hover:text-accent-foreground transition-colors cursor-pointer text-left"
  >
    {/* Avatar */}
    <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center text-xs font-semibold text-primary flex-shrink-0">
      {emp.fullName.charAt(0).toUpperCase()}
    </div>
    {/* Info */}
    <div className="flex-1 min-w-0">
      <div className="flex items-center gap-1.5">
        <span className="text-sm font-medium truncate">{emp.fullName}</span>
        <span className="text-xs text-muted-foreground font-mono flex-shrink-0">{emp.employeeCode}</span>
      </div>
      {(emp.position || emp.departmentName) && (
        <span className="text-xs text-muted-foreground truncate block">
          {[emp.position, emp.departmentName].filter(Boolean).join(' · ')}
        </span>
      )}
    </div>
    {isSelected && <Check className="h-3.5 w-3.5 text-primary flex-shrink-0" />}
  </button>
)
