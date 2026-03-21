import { useState, useMemo, useCallback, useRef } from 'react'
import type { Edge } from '@xyflow/react'
import type { OrgChartNodeDto } from '@/types/hr'
import type { OrgChartNode, OrgChartEdge } from './orgChartTypes'
import {
  transformToReactFlow,
  applyDagreLayout,
  getDescendantIds,
  getAncestorIds,
  computeNodeDepths,
} from './orgChartHelpers'

interface UseOrgChartLayoutOptions {
  apiNodes: OrgChartNodeDto[]
  direction?: 'TB' | 'LR'
  initialExpandLevel?: number
}

interface UseOrgChartLayoutReturn {
  nodes: OrgChartNode[]
  edges: Edge[]
  onToggleNode: (nodeId: string) => void
  onExpandAll: () => void
  onCollapseAll: () => void
  searchTerm: string
  setSearchTerm: (term: string) => void
  highlightedIds: Set<string>
}

export const useOrgChartLayout = ({
  apiNodes,
  direction = 'TB',
  initialExpandLevel = 2,
}: UseOrgChartLayoutOptions): UseOrgChartLayoutReturn => {
  const [expandedMap, setExpandedMap] = useState<Map<string, boolean>>(new Map())
  const [searchTerm, setSearchTerm] = useState('')
  const initializedRef = useRef(false)

  // Transform API data → React Flow nodes + edges
  const { baseNodes, hierarchyEdges, reportingEdges } = useMemo(() => {
    if (!apiNodes.length) {
      return { baseNodes: [] as OrgChartNode[], hierarchyEdges: [] as OrgChartEdge[], reportingEdges: [] as OrgChartEdge[] }
    }

    const result = transformToReactFlow(apiNodes)
    return {
      baseNodes: result.nodes,
      hierarchyEdges: result.hierarchyEdges,
      reportingEdges: result.reportingEdges,
    }
  }, [apiNodes])

  // Compute depths for initial expand level
  const depthMap = useMemo(() => computeNodeDepths(apiNodes), [apiNodes])

  // Initialize expandedMap when data arrives
  const effectiveExpandedMap = useMemo(() => {
    if (!baseNodes.length) return new Map<string, boolean>()

    // If user hasn't interacted, use initial expand level
    if (!initializedRef.current && expandedMap.size === 0) {
      const initial = new Map<string, boolean>()
      for (const node of baseNodes) {
        const depth = depthMap.get(node.id) ?? 0
        initial.set(node.id, depth < initialExpandLevel)
      }
      return initial
    }

    return expandedMap
  }, [baseNodes, expandedMap, depthMap, initialExpandLevel])

  // Search: find matching nodes and their highlighted IDs
  const highlightedIds = useMemo(() => {
    if (!searchTerm.trim()) return new Set<string>()
    const lower = searchTerm.toLowerCase()
    const matchIds = new Set<string>()

    for (const n of apiNodes) {
      if (
        n.name.toLowerCase().includes(lower) ||
        n.subtitle?.toLowerCase().includes(lower)
      ) {
        matchIds.add(n.id)
      }
    }

    return matchIds
  }, [apiNodes, searchTerm])

  // Ensure ancestors of highlighted nodes are expanded
  const expandedWithSearch = useMemo(() => {
    if (!highlightedIds.size) return effectiveExpandedMap

    const result = new Map(effectiveExpandedMap)
    for (const matchId of highlightedIds) {
      const ancestors = getAncestorIds(matchId, apiNodes)
      for (const ancestorId of ancestors) {
        result.set(ancestorId, true)
      }
    }
    return result
  }, [effectiveExpandedMap, highlightedIds, apiNodes])

  // Apply visibility + layout
  const { nodes, edges } = useMemo(() => {
    if (!baseNodes.length) return { nodes: [] as OrgChartNode[], edges: [] as Edge[] }

    // Determine hidden set: nodes whose any ancestor is collapsed
    const hiddenIds = new Set<string>()

    for (const [nodeId, isExpanded] of expandedWithSearch) {
      if (!isExpanded) {
        const descendants = getDescendantIds(nodeId, hierarchyEdges)
        for (const d of descendants) hiddenIds.add(d)
      }
    }

    // Filter to visible nodes
    const visibleNodes = baseNodes
      .filter((n) => !hiddenIds.has(n.id))
      .map((n) => ({
        ...n,
        data: {
          ...n.data,
          expanded: expandedWithSearch.get(n.id) ?? true,
          highlighted: highlightedIds.has(n.id),
        },
      })) as OrgChartNode[]

    // Filter edges: both source + target must be visible
    const visibleNodeIds = new Set(visibleNodes.map((n) => n.id))
    const visibleHierarchyEdges = hierarchyEdges.filter(
      (e) => visibleNodeIds.has(e.source) && visibleNodeIds.has(e.target),
    )
    const visibleReportingEdges = reportingEdges.filter(
      (e) => visibleNodeIds.has(e.source) && visibleNodeIds.has(e.target),
    )

    // Apply dagre layout (only on visible nodes + hierarchy edges)
    const layoutedNodes = applyDagreLayout(visibleNodes, visibleHierarchyEdges, direction)

    return {
      nodes: layoutedNodes,
      edges: [...visibleHierarchyEdges, ...visibleReportingEdges] as Edge[],
    }
  }, [baseNodes, hierarchyEdges, reportingEdges, expandedWithSearch, highlightedIds, direction])

  // Toggle expand/collapse for a specific node
  const onToggleNode = useCallback(
    (nodeId: string) => {
      initializedRef.current = true
      setExpandedMap((prev) => {
        const next = new Map(prev.size === 0 ? effectiveExpandedMap : prev)
        next.set(nodeId, !next.get(nodeId))
        return next
      })
    },
    [effectiveExpandedMap],
  )

  // Expand all nodes
  const onExpandAll = useCallback(() => {
    initializedRef.current = true
    setExpandedMap(() => {
      const all = new Map<string, boolean>()
      for (const node of baseNodes) {
        all.set(node.id, true)
      }
      return all
    })
  }, [baseNodes])

  // Collapse all (only roots expanded)
  const onCollapseAll = useCallback(() => {
    initializedRef.current = true
    setExpandedMap(() => {
      const collapsed = new Map<string, boolean>()
      for (const node of baseNodes) {
        const depth = depthMap.get(node.id) ?? 0
        collapsed.set(node.id, depth === 0)
      }
      return collapsed
    })
  }, [baseNodes, depthMap])

  return {
    nodes,
    edges,
    onToggleNode,
    onExpandAll,
    onCollapseAll,
    searchTerm,
    setSearchTerm,
    highlightedIds,
  }
}
