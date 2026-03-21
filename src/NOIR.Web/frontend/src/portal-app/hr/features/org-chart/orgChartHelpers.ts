import type { Edge, Node } from '@xyflow/react'
import { Position } from '@xyflow/react'
import dagre from '@dagrejs/dagre'
import type { OrgChartNodeDto } from '@/types/hr'
import type { OrgChartNode, OrgChartEdge, DepartmentNodeData, EmployeeNodeData } from './orgChartTypes'
import { NODE_WIDTH, NODE_HEIGHT } from './orgChartTypes'

// ─── Data transformation ────────────────────────────────────────────

interface TransformResult {
  nodes: OrgChartNode[]
  hierarchyEdges: OrgChartEdge[]
  reportingEdges: OrgChartEdge[]
  /** parentId → count of direct children (for expand toggle) */
  childCountMap: Map<string, number>
  /** managerId → count of direct reports (for expand toggle) */
  directReportCountMap: Map<string, number>
}

export const transformToReactFlow = (apiNodes: OrgChartNodeDto[]): TransformResult => {
  const childCountMap = new Map<string, number>()
  const directReportCountMap = new Map<string, number>()
  const hierarchyEdges: OrgChartEdge[] = []
  const reportingEdges: OrgChartEdge[] = []

  // Count children and direct reports
  for (const n of apiNodes) {
    if (n.parentId) {
      childCountMap.set(n.parentId, (childCountMap.get(n.parentId) ?? 0) + 1)
    }
    if (n.managerId) {
      directReportCountMap.set(n.managerId, (directReportCountMap.get(n.managerId) ?? 0) + 1)
    }
  }

  // Build nodes
  const nodes: OrgChartNode[] = apiNodes.map((n): OrgChartNode => {
    if (n.type === 'Department') {
      return {
        id: n.id,
        type: 'department',
        position: { x: 0, y: 0 },
        data: {
          nodeId: n.id,
          name: n.name,
          subtitle: n.subtitle ?? null,
          employeeCount: n.employeeCount ?? null,
          childCount: childCountMap.get(n.id) ?? 0,
          expanded: true,
          highlighted: false,
          onToggle: () => {},
        } satisfies DepartmentNodeData,
      } as Node<DepartmentNodeData, 'department'>
    }
    return {
      id: n.id,
      type: 'employee',
      position: { x: 0, y: 0 },
      data: {
        nodeId: n.id,
        name: n.name,
        subtitle: n.subtitle ?? null,
        avatarUrl: n.avatarUrl ?? null,
        status: n.status ?? null,
        directReportCount: directReportCountMap.get(n.id) ?? 0,
        expanded: true,
        highlighted: false,
        onToggle: () => {},
      } satisfies EmployeeNodeData,
    } as Node<EmployeeNodeData, 'employee'>
  })

  // Build hierarchy edges (parentId → id)
  for (const n of apiNodes) {
    if (n.parentId) {
      hierarchyEdges.push({
        id: `h-${n.parentId}-${n.id}`,
        source: n.parentId,
        target: n.id,
        type: 'smoothstep',
        data: { type: 'hierarchy' },
      })
    }
  }

  // Build reporting edges (managerId → id) — dashed, primary color
  for (const n of apiNodes) {
    if (n.managerId) {
      reportingEdges.push({
        id: `r-${n.managerId}-${n.id}`,
        source: n.managerId,
        target: n.id,
        type: 'smoothstep',
        style: {
          stroke: '#2563eb',
          strokeWidth: 2,
          strokeDasharray: '6 4',
        },
        data: { type: 'reporting' },
      })
    }
  }

  return { nodes, hierarchyEdges, reportingEdges, childCountMap, directReportCountMap }
}

// ─── Dagre layout ───────────────────────────────────────────────────

export const applyDagreLayout = (
  nodes: OrgChartNode[],
  hierarchyEdges: Edge[],
  direction: 'TB' | 'LR' = 'TB',
): OrgChartNode[] => {
  const g = new dagre.graphlib.Graph().setDefaultEdgeLabel(() => ({}))
  g.setGraph({ rankdir: direction, ranksep: 60, nodesep: 30 })

  for (const node of nodes) {
    g.setNode(node.id, { width: NODE_WIDTH, height: NODE_HEIGHT })
  }

  for (const edge of hierarchyEdges) {
    // Only add edge if both source and target exist in the node set
    if (g.hasNode(edge.source) && g.hasNode(edge.target)) {
      g.setEdge(edge.source, edge.target)
    }
  }

  dagre.layout(g)

  const isHorizontal = direction === 'LR'

  return nodes.map((node) => {
    const dagreNode = g.node(node.id)
    if (!dagreNode) return node

    return {
      ...node,
      position: {
        x: dagreNode.x - NODE_WIDTH / 2,
        y: dagreNode.y - NODE_HEIGHT / 2,
      },
      targetPosition: isHorizontal ? Position.Left : Position.Top,
      sourcePosition: isHorizontal ? Position.Right : Position.Bottom,
    } as OrgChartNode
  })
}

// ─── Tree traversal helpers ─────────────────────────────────────────

/** Get all descendant IDs via hierarchy edges (BFS) */
export const getDescendantIds = (
  nodeId: string,
  hierarchyEdges: Edge[],
): Set<string> => {
  const result = new Set<string>()
  const queue = [nodeId]
  const childMap = new Map<string, string[]>()

  for (const e of hierarchyEdges) {
    const children = childMap.get(e.source) ?? []
    children.push(e.target)
    childMap.set(e.source, children)
  }

  while (queue.length > 0) {
    const current = queue.shift()!
    const children = childMap.get(current) ?? []
    for (const child of children) {
      if (!result.has(child)) {
        result.add(child)
        queue.push(child)
      }
    }
  }

  return result
}

/** Get ancestor IDs walking up parentId chain */
export const getAncestorIds = (
  nodeId: string,
  apiNodes: OrgChartNodeDto[],
): string[] => {
  const nodeMap = new Map(apiNodes.map((n) => [n.id, n]))
  const ancestors: string[] = []
  let current = nodeMap.get(nodeId)

  while (current?.parentId) {
    ancestors.push(current.parentId)
    current = nodeMap.get(current.parentId)
  }

  return ancestors
}

/** Calculate depth of each node from root(s) */
export const computeNodeDepths = (
  apiNodes: OrgChartNodeDto[],
): Map<string, number> => {
  const depthMap = new Map<string, number>()
  const childMap = new Map<string, string[]>()

  for (const n of apiNodes) {
    if (n.parentId) {
      const children = childMap.get(n.parentId) ?? []
      children.push(n.id)
      childMap.set(n.parentId, children)
    }
  }

  // BFS from root nodes
  const roots = apiNodes.filter((n) => !n.parentId)
  const queue: Array<{ id: string; depth: number }> = roots.map((n) => ({ id: n.id, depth: 0 }))

  while (queue.length > 0) {
    const { id, depth } = queue.shift()!
    if (depthMap.has(id)) continue
    depthMap.set(id, depth)

    for (const childId of childMap.get(id) ?? []) {
      queue.push({ id: childId, depth: depth + 1 })
    }
  }

  return depthMap
}
