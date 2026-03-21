import type { Node, Edge, BuiltInNode } from '@xyflow/react'
import type { EmployeeStatus } from '@/types/hr'

/** Data payload for department nodes in the org chart */
export type DepartmentNodeData = {
  nodeId: string
  name: string
  subtitle: string | null
  employeeCount: number | null
  childCount: number
  expanded: boolean
  highlighted: boolean
  onToggle: (id: string) => void
}

/** Data payload for employee nodes in the org chart */
export type EmployeeNodeData = {
  nodeId: string
  name: string
  subtitle: string | null
  avatarUrl: string | null
  status: EmployeeStatus | null
  directReportCount: number
  expanded: boolean
  highlighted: boolean
  onToggle: (id: string) => void
}

export type DepartmentNodeType = Node<DepartmentNodeData, 'department'>
export type EmployeeNodeType = Node<EmployeeNodeData, 'employee'>
export type OrgChartNode = DepartmentNodeType | EmployeeNodeType | BuiltInNode

export type OrgChartEdge = Edge & { data?: { type: 'hierarchy' | 'reporting' } }

/** Status → getStatusBadgeClasses color key */
export const statusColorMap: Record<string, 'green' | 'yellow' | 'gray' | 'red'> = {
  Active: 'green',
  Suspended: 'yellow',
  Resigned: 'gray',
  Terminated: 'red',
}

export const NODE_WIDTH = 280
export const NODE_HEIGHT = 80
