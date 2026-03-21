import { memo } from 'react'
import { Handle, Position, NodeToolbar } from '@xyflow/react'
import type { NodeProps } from '@xyflow/react'
import { ChevronDown, ChevronUp, Eye } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { Avatar, Badge, Button } from '@uikit'
import { cn } from '@/lib/utils'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import type { EmployeeNodeType } from './orgChartTypes'
import { statusColorMap } from './orgChartTypes'

export const EmployeeNode = memo(({ data, selected }: NodeProps<EmployeeNodeType>) => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()

  const colorKey = data.status ? statusColorMap[data.status] : undefined

  return (
    <>
      <NodeToolbar isVisible={selected} position={Position.Top} offset={8}>
        <div className="flex items-center gap-1 rounded-lg border bg-card p-1 shadow-md">
          <Button
            size="sm"
            variant="ghost"
            className="h-7 cursor-pointer"
            onClick={() => navigate(`/portal/hr/employees/${data.nodeId}`)}
          >
            <Eye className="mr-1 size-3.5" />
            {t('hr.orgChart.viewProfile', 'View Profile')}
          </Button>
        </div>
      </NodeToolbar>

      <Handle type="target" position={Position.Top} className="!bg-border" />

      <div
        className={cn(
          'flex w-[280px] items-center gap-3 rounded-xl border bg-card p-4 shadow-sm',
          'transition-all duration-300 hover:shadow-lg',
          selected && 'ring-2 ring-primary',
          data.highlighted && 'ring-2 ring-primary ring-offset-2',
        )}
      >
        {/* Avatar */}
        <Avatar
          src={data.avatarUrl ?? undefined}
          alt={data.name}
          fallback={data.name}
          size="md"
        />

        {/* Content */}
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="truncate text-sm font-semibold">{data.name}</span>
            {data.status && colorKey && (
              <Badge
                variant="outline"
                className={cn('px-1.5 py-0 text-[10px]', getStatusBadgeClasses(colorKey))}
              >
                {data.status}
              </Badge>
            )}
          </div>
          {data.subtitle && (
            <div className="mt-0.5 truncate text-xs text-muted-foreground">
              {data.subtitle}
            </div>
          )}
        </div>

        {/* Expand/collapse toggle for managers with direct reports */}
        {data.directReportCount > 0 && (
          <button
            className="nodrag nopan flex cursor-pointer items-center gap-1 rounded-md border bg-card px-1.5 py-0.5 text-[11px] text-muted-foreground hover:bg-accent"
            onClick={(e) => {
              e.stopPropagation()
              data.onToggle(data.nodeId)
            }}
            aria-label={data.expanded ? t('hr.orgChart.collapseAll', 'Collapse') : t('hr.orgChart.expandAll', 'Expand')}
          >
            {data.expanded ? <ChevronUp className="size-3" /> : <ChevronDown className="size-3" />}
            {data.directReportCount}
          </button>
        )}
      </div>

      <Handle type="source" position={Position.Bottom} className="!bg-border" />
    </>
  )
})

EmployeeNode.displayName = 'EmployeeNode'
