import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Filter, Check, Tag, UserX, Calendar, X, ChevronDown, ChevronUp } from 'lucide-react'
import { Input, Popover, PopoverContent, PopoverTrigger, Avatar } from '@uikit'
import type { ProjectMemberDto, TaskLabelBriefDto } from '@/types/pm'

// ─── Filter types ─────────────────────────────────────────────────────────────

export type DueDateFilter = '' | 'no-date' | 'overdue' | 'today' | 'next-7' | 'next-30' | 'specific'

export type CompletionFilter = '' | 'completed' | 'active'

// ─── Shared filter logic ──────────────────────────────────────────────────────

export const matchDueDate = (
  dueDate: string | null,
  filter: DueDateFilter,
  specificStart?: string,
  specificEnd?: string,
): boolean => {
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
  if (filter === 'specific') {
    if (specificStart && specificEnd) {
      const start = new Date(specificStart); start.setHours(0, 0, 0, 0)
      const end = new Date(specificEnd); end.setHours(23, 59, 59, 999)
      return due >= start && due <= end
    }
    if (specificStart) {
      const start = new Date(specificStart); start.setHours(0, 0, 0, 0)
      return due.getTime() === start.getTime()
    }
  }
  return true
}

export const matchCompletion = (
  completedAt: string | null | undefined,
  filter: CompletionFilter,
): boolean => {
  if (!filter) return true
  if (filter === 'completed') return !!completedAt
  if (filter === 'active') return !completedAt
  return true
}

// ─── Internal sub-components ─────────────────────────────────────────────────

const SectionHeader = ({
  label,
  collapsible = false,
  collapsed = false,
  onToggle,
}: {
  label: string
  collapsible?: boolean
  collapsed?: boolean
  onToggle?: () => void
}) => (
  <button
    onClick={collapsible ? onToggle : undefined}
    className={`flex items-center justify-between w-full px-1 mb-1.5 ${collapsible ? 'cursor-pointer group' : ''}`}
  >
    <p className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wide">
      {label}
    </p>
    {collapsible && (
      <span className="text-muted-foreground/60 group-hover:text-muted-foreground transition-colors">
        {collapsed
          ? <ChevronDown className="h-3 w-3" />
          : <ChevronUp className="h-3 w-3" />}
      </span>
    )}
  </button>
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

// ─── DateInput ────────────────────────────────────────────────────────────────

const DateInput = ({
  value,
  onChange,
  placeholder,
}: {
  value: string
  onChange: (v: string) => void
  placeholder?: string
}) => {
  const { t } = useTranslation('common')
  return (
    <div className="relative">
      <Input
        type="date"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="h-8 pl-2 pr-7 text-sm cursor-pointer [color-scheme:light] dark:[color-scheme:dark]"
      />
      {value && (
        <button
          onClick={() => onChange('')}
          className="absolute right-1.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
          aria-label={t('labels.clearDate', { defaultValue: 'Clear date' })}
        >
          <X className="h-3 w-3" />
        </button>
      )}
    </div>
  )
}

// ─── Props ───────────────────────────────────────────────────────────────────

interface TaskFilterPopoverProps {
  // Which sections to render
  showAssignees?: boolean
  showReporters?: boolean
  showLabels?: boolean
  showDueDate?: boolean
  showCompletion?: boolean
  // Data
  members?: ProjectMemberDto[]
  availableLabels?: TaskLabelBriefDto[]
  // State — assignees
  selectedAssignees?: string[]
  onAssigneesChange?: (v: string[]) => void
  // State — reporters
  selectedReporters?: string[]
  onReportersChange?: (v: string[]) => void
  // State — labels
  selectedLabels?: string[]
  onLabelsChange?: (v: string[]) => void
  // State — due date
  selectedDueDate?: DueDateFilter
  onDueDateChange?: (v: DueDateFilter) => void
  dueDateSpecificStart?: string
  onDueDateSpecificStartChange?: (v: string) => void
  dueDateSpecificEnd?: string
  onDueDateSpecificEndChange?: (v: string) => void
  // State — completion
  completionFilter?: CompletionFilter
  onCompletionChange?: (v: CompletionFilter) => void
  // Meta
  onClearAll: () => void
  activeCount: number
}

// ─── TaskFilterPopover ────────────────────────────────────────────────────────

export const TaskFilterPopover = ({
  showAssignees = false,
  showReporters = false,
  showLabels = false,
  showDueDate = false,
  showCompletion = false,
  members = [],
  availableLabels = [],
  selectedAssignees = [],
  onAssigneesChange,
  selectedReporters = [],
  onReportersChange,
  selectedLabels = [],
  onLabelsChange,
  selectedDueDate = '',
  onDueDateChange,
  dueDateSpecificStart = '',
  onDueDateSpecificStartChange,
  dueDateSpecificEnd = '',
  onDueDateSpecificEndChange,
  completionFilter = '',
  onCompletionChange,
  onClearAll,
  activeCount,
}: TaskFilterPopoverProps) => {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)
  const [labelsCollapsed, setLabelsCollapsed] = useState(false)

  // ── Assignees ──────────────────────────────────────────────────────────────
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

  // ── Reporters ─────────────────────────────────────────────────────────────
  const toggleReporter = (key: string) => {
    if (!onReportersChange) return
    const next = selectedReporters.includes(key)
      ? selectedReporters.filter((r) => r !== key)
      : [...selectedReporters, key]
    onReportersChange(next)
  }

  const isReporterActive = (name: string) =>
    selectedReporters.some(
      (r) => r !== '__no-reporter__' && name.toLowerCase().includes(r.toLowerCase()),
    )

  // ── Labels ─────────────────────────────────────────────────────────────────
  const toggleLabel = (id: string) => {
    if (!onLabelsChange) return
    const next = selectedLabels.includes(id)
      ? selectedLabels.filter((l) => l !== id)
      : [...selectedLabels, id]
    onLabelsChange(next)
  }

  // ── Due Date ───────────────────────────────────────────────────────────────
  const handleDueDate = (key: DueDateFilter) => {
    if (!onDueDateChange) return
    if (key === 'specific') {
      // Toggle specific mode — keep dates if re-selecting
      onDueDateChange(selectedDueDate === 'specific' ? '' : 'specific')
    } else {
      // Selecting a quick option clears specific dates
      if (selectedDueDate === key) {
        onDueDateChange('')
      } else {
        onDueDateChange(key)
        onDueDateSpecificStartChange?.('')
        onDueDateSpecificEndChange?.('')
      }
    }
  }

  // ── Completion ─────────────────────────────────────────────────────────────
  const handleCompletion = (key: CompletionFilter) => {
    if (!onCompletionChange) return
    onCompletionChange(completionFilter === key ? '' : key)
  }

  const dueDateQuickOptions: { key: DueDateFilter; label: string; dot: string }[] = [
    { key: 'no-date', label: t('pm.filterNoDueDate', { defaultValue: 'No due date' }), dot: 'bg-muted-foreground/60' },
    { key: 'overdue', label: t('pm.filterOverdue', { defaultValue: 'Overdue' }), dot: 'bg-red-500' },
    { key: 'today', label: t('pm.filterDueToday', { defaultValue: 'Due today' }), dot: 'bg-yellow-500' },
    { key: 'next-7', label: t('pm.filterDueNext7', { defaultValue: 'Next 7 days' }), dot: 'bg-blue-500' },
    { key: 'next-30', label: t('pm.filterDueNext30', { defaultValue: 'Next 30 days' }), dot: 'bg-green-500' },
  ]

  const hasAnySection = showCompletion || showAssignees || showReporters || showDueDate || showLabels

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-medium border cursor-pointer transition-all ${
            activeCount > 0
              ? 'bg-primary/5 text-primary border-primary/30 hover:bg-primary/10'
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

      <PopoverContent align="start" className="w-[300px] p-0" sideOffset={6}>
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

        <div className="max-h-[calc(100vh-200px)] overflow-y-auto divide-y divide-border">

          {/* ── Completion ── */}
          {showCompletion && (
            <div className="p-3">
              <SectionHeader label={t('pm.filterCompletion', { defaultValue: 'Completion' })} />
              <div className="space-y-0.5">
                <CheckRow
                  checked={completionFilter === 'completed'}
                  onClick={() => handleCompletion('completed')}
                >
                  <div className="h-4 w-4 rounded-full border-2 border-green-500 flex items-center justify-center flex-shrink-0">
                    <Check className="h-2.5 w-2.5 text-green-500" />
                  </div>
                  <span className="text-sm">{t('pm.filterCompleted', { defaultValue: 'Marked as complete' })}</span>
                </CheckRow>
                <CheckRow
                  checked={completionFilter === 'active'}
                  onClick={() => handleCompletion('active')}
                >
                  <div className="h-4 w-4 rounded-full border-2 border-muted-foreground/50 flex-shrink-0" />
                  <span className="text-sm">{t('pm.filterNotCompleted', { defaultValue: 'Not marked as complete' })}</span>
                </CheckRow>
              </div>
            </div>
          )}

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

          {/* ── Reporters ── */}
          {showReporters && (
            <div className="p-3">
              <SectionHeader label={t('pm.filterReporters', { defaultValue: 'Reporters' })} />
              <div className="space-y-0.5">
                <CheckRow
                  checked={selectedReporters.includes('__no-reporter__')}
                  onClick={() => toggleReporter('__no-reporter__')}
                >
                  <div className="h-5 w-5 rounded-full bg-muted border border-border flex items-center justify-center flex-shrink-0">
                    <UserX className="h-3 w-3 text-muted-foreground" />
                  </div>
                  <span className="text-sm text-muted-foreground">
                    {t('pm.filterNoReporter', { defaultValue: 'No reporter' })}
                  </span>
                </CheckRow>
                {members.map((member) => (
                  <CheckRow
                    key={member.id}
                    checked={isReporterActive(member.employeeName)}
                    onClick={() => toggleReporter(member.employeeName.split(' ')[0])}
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

          {/* ── Due Date ── */}
          {showDueDate && (
            <div className="p-3">
              <SectionHeader label={t('pm.filterDueDate', { defaultValue: 'Due date' })} />
              <div className="space-y-0.5">
                {dueDateQuickOptions.map(({ key, label, dot }) => (
                  <RadioRow
                    key={key}
                    selected={selectedDueDate === key}
                    onClick={() => handleDueDate(key)}
                  >
                    <span className={`h-2 w-2 rounded-full flex-shrink-0 mt-0.5 ${dot}`} />
                    <span className="text-sm">{label}</span>
                  </RadioRow>
                ))}

                {/* Specific date / range */}
                <RadioRow
                  selected={selectedDueDate === 'specific'}
                  onClick={() => handleDueDate('specific')}
                >
                  <Calendar className="h-3.5 w-3.5 text-muted-foreground flex-shrink-0" />
                  <span className="text-sm">{t('pm.filterSpecificDate', { defaultValue: 'Specific date / range' })}</span>
                </RadioRow>

                {selectedDueDate === 'specific' && (
                  <div className="mt-2 space-y-1.5 pl-6">
                    <div>
                      <p className="text-[10px] text-muted-foreground mb-1">
                        {t('pm.filterDateFrom', { defaultValue: 'From' })}
                      </p>
                      <DateInput
                        value={dueDateSpecificStart}
                        onChange={(v) => onDueDateSpecificStartChange?.(v)}
                      />
                    </div>
                    <div>
                      <p className="text-[10px] text-muted-foreground mb-1">
                        {t('pm.filterDateTo', { defaultValue: 'To (optional)' })}
                      </p>
                      <DateInput
                        value={dueDateSpecificEnd}
                        onChange={(v) => onDueDateSpecificEndChange?.(v)}
                      />
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* ── Labels ── */}
          {showLabels && (
            <div className="p-3">
              <SectionHeader
                label={t('pm.filterLabels', { defaultValue: 'Labels' })}
                collapsible={availableLabels.length > 0}
                collapsed={labelsCollapsed}
                onToggle={() => setLabelsCollapsed((v) => !v)}
              />
              {!labelsCollapsed && (
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
              )}
            </div>
          )}

          {/* Empty state */}
          {!hasAnySection && (
            <div className="p-4 text-center">
              <Filter className="h-8 w-8 mx-auto text-muted-foreground mb-2" />
              <p className="text-xs text-muted-foreground">{t('pm.noFiltersAvailable', { defaultValue: 'No filters available' })}</p>
            </div>
          )}
        </div>
      </PopoverContent>
    </Popover>
  )
}
