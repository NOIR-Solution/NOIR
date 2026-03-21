import { memo } from 'react'
import { Handle, Position, NodeToolbar } from '@xyflow/react'
import type { NodeProps } from '@xyflow/react'
import { Building2, ChevronDown, ChevronUp } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { Badge, Button } from '@uikit'
import { cn } from '@/lib/utils'
import type { DepartmentNodeType } from './orgChartTypes'

export const DepartmentNode = memo(({ data, selected }: NodeProps<DepartmentNodeType>) => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()

  return (
    <>
      <NodeToolbar isVisible={selected} position={Position.Top} offset={8}>
        <div className="flex items-center gap-1 rounded-lg border bg-card p-1 shadow-md">
          <Button
            size="sm"
            variant="ghost"
            className="h-7 cursor-pointer"
            onClick={() => navigate('/portal/hr/departments')}
          >
            {t('hr.orgChart.viewDepartments', 'View Departments')}
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
        {/* Icon */}
        <div className="flex size-10 flex-shrink-0 items-center justify-center rounded-full bg-primary/10">
          <Building2 className="size-[18px] text-primary" />
        </div>

        {/* Content */}
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="truncate text-sm font-semibold">{data.name}</span>
            {data.employeeCount != null && (
              <Badge variant="outline" className="px-2 py-0 text-[11px]">
                {data.employeeCount}
              </Badge>
            )}
          </div>
          {data.subtitle && (
            <div className="mt-0.5 truncate text-xs text-muted-foreground">
              {data.subtitle}
            </div>
          )}
        </div>

        {/* Expand/collapse toggle */}
        {data.childCount > 0 && (
          <button
            className="nodrag nopan flex cursor-pointer items-center gap-1 rounded-md border bg-card px-1.5 py-0.5 text-[11px] text-muted-foreground hover:bg-accent"
            onClick={(e) => {
              e.stopPropagation()
              data.onToggle(data.nodeId)
            }}
            aria-label={data.expanded ? t('hr.orgChart.collapseAll', 'Collapse') : t('hr.orgChart.expandAll', 'Expand')}
          >
            {data.expanded ? <ChevronUp className="size-3" /> : <ChevronDown className="size-3" />}
            {data.childCount}
          </button>
        )}
      </div>

      <Handle type="source" position={Position.Bottom} className="!bg-border" />
    </>
  )
})

DepartmentNode.displayName = 'DepartmentNode'
