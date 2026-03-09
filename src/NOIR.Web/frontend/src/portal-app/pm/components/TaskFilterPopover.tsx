import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Filter, Check, User, Tag, UserX } from 'lucide-react'
import { Popover, PopoverContent, PopoverTrigger, Avatar } from '@uikit'
import type { ProjectMemberDto, TaskLabelBriefDto } from '@/types/pm'

// ─── Due Date Filter type ─────────────────────────────────────────────────────

export type DueDateFilter = '' | 'no-date' | 'overdue' | 'today' | 'next-7' | 'next-30'

// ─── Shared filter logic ──────────────────────────────────────────────────────

export const matchDueDate = (dueDate: string | null, filter: DueDateFilter): boolean => {
  if (!filter) return true
  const today = new Date(); today.setHours(0, 0, 0, 0)
  const next7 = new Date(today); next7.setDate(today.getDate() + 7)
  const next30 = new Date(today); next30.setDate(today.getDate() + 30)
  if (filter === 'no-date') return !dueDate
  if (!dueDate) return false
  const due = new Date(dueDate); due.setHours(0, 0, 0, 0)
  if (filter === 'overdue') return due < today
  if (filter === 'today') return due.getTime() === today.getTime()
  if (filter === 'next-7') return due >= today && due <= next7
  if (filter === 'next-30') return due >= today && due <= next30
  return true
}

// ─── Internal sub-components ─────────────────────────────────────────────────

const SectionHeader = ({ label }: { label: string }) => (
  <p className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wide px-1 mb-1.5">
    {label}
  </p>
)

const CheckRow = ({
  checked,
  onClick,
  children,
}: {
  checked: boolean
  onClick: () => void
  children: React.ReactNode
}) => (
  <button
    onClick={onClick}
    className="flex items-center gap-2.5 w-full px-2 py-1.5 rounded-md hover:bg-muted cursor-pointer text-left group transition-colors"
  >
    <div
      className={`w-4 h-4 rounded border-2 flex items-center justify-center flex-shrink-0 transition-all ${
        checked ? 'bg-primary border-primary' : 'border-border group-hover:border-primary/40'
      }`}
    >
      {checked && <Check className="h-2.5 w-2.5 text-primary-foreground" />}
    </div>
    {children}
  </button>
)

const RadioRow = ({
  selected,
  onClick,
  children,
}: {
  selected: boolean
  onClick: () => void
  children: React.ReactNode
}) => (
  <button
    onClick={onClick}
    className="flex items-center gap-2.5 w-full px-2 py-1.5 rounded-md hover:bg-muted cursor-pointer text-left group transition-colors"
  >
    <div
      className={`w-4 h-4 rounded-full border-2 flex items-center justify-center flex-shrink-0 transition-all ${
        selected ? 'border-primary' : 'border-border group-hover:border-primary/40'
      }`}
    >
      {selected && <div className="w-2 h-2 rounded-full bg-primary" />}
    </div>
    {children}
  </button>
)

// ─── Props ───────────────────────────────────────────────────────────────────

interface TaskFilterPopoverProps {
  // Which sections to render
  showAssignees?: boolean
  showLabels?: boolean
  showDueDate?: boolean
  // Data
  members?: ProjectMemberDto[]
  availableLabels?: TaskLabelBriefDto[]
  // State
  selectedAssignees?: string[]  // member.employeeName (first word) or '__unassigned__'
  selectedLabels?: string[]     // label ids or '__no-label__'
  selectedDueDate?: DueDateFilter
  // Handlers
  onAssigneesChange?: (v: string[]) => void
  onLabelsChange?: (v: string[]) => void
  onDueDateChange?: (v: DueDateFilter) => void
  onClearAll: () => void
  activeCount: number
}

// ─── TaskFilterPopover ────────────────────────────────────────────────────────

export const TaskFilterPopover = ({
  showAssignees = false,
  showLabels = false,
  showDueDate = false,
  members = [],
  availableLabels = [],
  selectedAssignees = [],
  selectedLabels = [],
  selectedDueDate = '',
  onAssigneesChange,
  onLabelsChange,
  onDueDateChange,
  onClearAll,
  activeCount,
}: TaskFilterPopoverProps) => {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)

  const toggleAssignee = (key: string) => {
    if (!onAssigneesChange) return
    const next = selectedAssignees.includes(key)
      ? selectedAssignees.filter((a) => a !== key)
      : [...selectedAssignees, key]
    onAssigneesChange(next)
  }

  const isAssigneeActive = (name: string) =>
    selectedAssignees.some(
      (a) => a !== '__unassigned__' && name.toLowerCase().includes(a.toLowerCase()),
    )

  const toggleLabel = (id: string) => {
    if (!onLabelsChange) return
    const next = selectedLabels.includes(id)
      ? selectedLabels.filter((l) => l !== id)
      : [...selectedLabels, id]
    onLabelsChange(next)
  }

  const handleDueDate = (key: DueDateFilter) => {
    if (!onDueDateChange) return
    onDueDateChange(selectedDueDate === key ? '' : key)
  }

  const dueDateOptions: { key: DueDateFilter; label: string; dot: string }[] = [
    { key: 'no-date', label: t('pm.filterNoDueDate', { defaultValue: 'No due date' }), dot: 'bg-muted-foreground/60' },
    { key: 'overdue', label: t('pm.filterOverdue', { defaultValue: 'Overdue' }), dot: 'bg-red-500' },
    { key: 'today', label: t('pm.filterDueToday', { defaultValue: 'Due today' }), dot: 'bg-yellow-500' },
    { key: 'next-7', label: t('pm.filterDueNext7', { defaultValue: 'Next 7 days' }), dot: 'bg-blue-500' },
    { key: 'next-30', label: t('pm.filterDueNext30', { defaultValue: 'Next 30 days' }), dot: 'bg-green-500' },
  ]

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          className={`inline-flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-medium border cursor-pointer transition-all ${
            activeCount > 0
              ? 'bg-primary/10 text-primary border-primary/30 hover:bg-primary/15'
              : 'bg-background border-border text-foreground hover:bg-muted'
          }`}
        >
          <Filter className="h-3.5 w-3.5" />
          {t('pm.moreFilters', { defaultValue: 'Filters' })}
          {activeCount > 0 && (
            <span className="inline-flex items-center justify-center h-4 min-w-4 px-1 rounded-full text-[10px] font-bold leading-[1.1] bg-primary text-primary-foreground">
              {activeCount}
            </span>
          )}
        </button>
      </PopoverTrigger>

      <PopoverContent align="start" className="w-72 p-0" sideOffset={6}>
        {/* Header */}
        <div className="flex items-center justify-between px-3 py-2.5 border-b">
          <span className="text-sm font-semibold">
            {t('pm.moreFilters', { defaultValue: 'Filters' })}
          </span>
          {activeCount > 0 && (
            <button
              onClick={() => { onClearAll(); setOpen(false) }}
              className="text-xs text-muted-foreground hover:text-foreground cursor-pointer transition-colors"
            >
              {t('buttons.clearAll', { defaultValue: 'Clear all' })}
            </button>
          )}
        </div>

        <div className="max-h-[440px] overflow-y-auto divide-y divide-border">
          {/* ── Assignees ── */}
          {showAssignees && (
            <div className="p-3">
              <SectionHeader label={t('pm.filterAssignees', { defaultValue: 'Assignees' })} />
              <div className="space-y-0.5">
                <CheckRow
                  checked={selectedAssignees.includes('__unassigned__')}
                  onClick={() => toggleAssignee('__unassigned__')}
                >
                  <div className="h-5 w-5 rounded-full bg-muted border border-border flex items-center justify-center flex-shrink-0">
                    <UserX className="h-3 w-3 text-muted-foreground" />
                  </div>
                  <span className="text-sm text-muted-foreground">
                    {t('pm.filterNoAssignee', { defaultValue: 'No assignee' })}
                  </span>
                </CheckRow>
                {members.map((member) => (
                  <CheckRow
                    key={member.id}
                    checked={isAssigneeActive(member.employeeName)}
                    onClick={() => toggleAssignee(member.employeeName.split(' ')[0])}
                  >
                    <Avatar
                      src={member.avatarUrl ?? undefined}
                      alt={member.employeeName}
                      fallback={member.employeeName}
                      size="sm"
                      className="h-5 w-5 text-[8px] flex-shrink-0"
                    />
                    <span className="text-sm truncate">{member.employeeName}</span>
                  </CheckRow>
                ))}
              </div>
            </div>
          )}

          {/* ── Labels ── */}
          {showLabels && (
            <div className="p-3">
              <SectionHeader label={t('pm.filterLabels', { defaultValue: 'Labels' })} />
              <div className="space-y-0.5">
                <CheckRow
                  checked={selectedLabels.includes('__no-label__')}
                  onClick={() => toggleLabel('__no-label__')}
                >
                  <Tag className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                  <span className="text-sm text-muted-foreground">
                    {t('pm.filterNoLabels', { defaultValue: 'No labels' })}
                  </span>
                </CheckRow>
                {availableLabels.length === 0 ? (
                  <p className="text-xs text-muted-foreground px-2 py-1.5 italic">
                    {t('pm.noLabelsYet', { defaultValue: 'No labels on this project yet' })}
                  </p>
                ) : (
                  availableLabels.map((label) => (
                    <CheckRow
                      key={label.id}
                      checked={selectedLabels.includes(label.id)}
                      onClick={() => toggleLabel(label.id)}
                    >
                      <span
                        className="h-3.5 w-3.5 rounded-sm flex-shrink-0 border border-black/10"
                        style={{ backgroundColor: label.color || '#94a3b8' }}
                      />
                      <span className="text-sm truncate">{label.name}</span>
                    </CheckRow>
                  ))
                )}
              </div>
            </div>
          )}

          {/* ── Due Date ── */}
          {showDueDate && (
            <div className="p-3">
              <SectionHeader label={t('pm.filterDueDate', { defaultValue: 'Due date' })} />
              <div className="space-y-0.5">
                {dueDateOptions.map(({ key, label, dot }) => (
                  <RadioRow
                    key={key}
                    selected={selectedDueDate === key}
                    onClick={() => handleDueDate(key)}
                  >
                    <span className={`h-2 w-2 rounded-full flex-shrink-0 mt-0.5 ${dot}`} />
                    <span className="text-sm">{label}</span>
                  </RadioRow>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Empty state */}
        {!showAssignees && !showLabels && !showDueDate && (
          <div className="p-4 text-center">
            <User className="h-8 w-8 mx-auto text-muted-foreground/40 mb-2" />
            <p className="text-xs text-muted-foreground">{t('pm.noFiltersAvailable', { defaultValue: 'No filters available' })}</p>
          </div>
        )}
      </PopoverContent>
    </Popover>
  )
}
