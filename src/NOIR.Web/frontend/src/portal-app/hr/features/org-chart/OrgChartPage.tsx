import { useState, useCallback, useMemo, useTransition, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  ReactFlow,
  Background,
  Controls,
  MiniMap,
  Panel,
  BackgroundVariant,
  ReactFlowProvider,
  useReactFlow,
} from '@xyflow/react'
import type { Node, NodeMouseHandler, NodeTypes, OnNodesChange } from '@xyflow/react'
import '@xyflow/react/dist/style.css'
import {
  GitBranch,
  Search,
  Users,
  Maximize2,
  Minimize2,
} from 'lucide-react'
import {
  Button,
  Card,
  CardContent,
  EmptyState,
  Input,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
} from '@uikit'
import { useOrgChartQuery, useDepartmentsQuery } from '@/portal-app/hr/queries'
import { DepartmentNode } from './DepartmentNode'
import { EmployeeNode } from './EmployeeNode'
import { useOrgChartLayout } from './useOrgChartLayout'

// nodeTypes MUST be defined outside the component to avoid re-renders
const nodeTypes: NodeTypes = {
  department: DepartmentNode,
  employee: EmployeeNode,
} as NodeTypes

const defaultEdgeOptions = {
  type: 'smoothstep' as const,
  style: { stroke: '#94a3b8', strokeWidth: 2 },
}

const MINIMAP_THRESHOLD = 20

/** Inner component that uses useReactFlow (must be inside ReactFlowProvider) */
const OrgChartCanvas = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { fitView } = useReactFlow()
  const [departmentFilter, setDepartmentFilter] = useState<string>('all')
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null)
  const [isFilterPending, startFilterTransition] = useTransition()

  const deptId = departmentFilter !== 'all' ? departmentFilter : undefined
  const { data: orgChartData, isLoading, isFetching, isPlaceholderData } = useOrgChartQuery(deptId)

  const handleDepartmentFilterChange = useCallback((value: string) => {
    startFilterTransition(() => {
      setDepartmentFilter(value)
      setSelectedNodeId(null)
    })
  }, [])

  // Fit view with animation when fresh data arrives (skip placeholder data)
  useEffect(() => {
    if (orgChartData?.length && !isPlaceholderData) {
      requestAnimationFrame(() => fitView({ duration: 400, padding: 0.15 }))
    }
  }, [orgChartData, isPlaceholderData, fitView])
  const { data: departments } = useDepartmentsQuery()

  const flatDepts = useMemo(() => {
    const flatten = (nodes: typeof departments, prefix = ''): { id: string; name: string }[] => {
      if (!nodes) return []
      return nodes.flatMap((node) => [
        { id: node.id, name: prefix + node.name },
        ...flatten(node.children, prefix + '  '),
      ])
    }
    return flatten(departments)
  }, [departments])

  const apiNodes = useMemo(() => orgChartData ?? [], [orgChartData])

  const {
    nodes,
    edges,
    onToggleNode,
    onExpandAll,
    onCollapseAll,
    searchTerm,
    setSearchTerm,
  } = useOrgChartLayout({
    apiNodes,
    direction: 'TB',
    initialExpandLevel: 2,
  })

  // Inject onToggle callback + selected state into every node
  const nodesWithCallbacks = useMemo(
    () =>
      nodes.map((node) => ({
        ...node,
        selected: node.id === selectedNodeId,
        data: { ...node.data, onToggle: onToggleNode },
      })),
    [nodes, onToggleNode, selectedNodeId],
  )

  // Handle selection changes from React Flow (controlled mode)
  const handleNodesChange: OnNodesChange = useCallback((changes) => {
    for (const change of changes) {
      if (change.type === 'select') {
        setSelectedNodeId(change.selected ? change.id : null)
      }
    }
  }, [])

  const handleNodeDoubleClick: NodeMouseHandler = useCallback(
    (_event: React.MouseEvent, node: Node) => {
      if (node.type === 'employee') {
        navigate(`/portal/hr/employees/${node.id}`)
      }
    },
    [navigate],
  )

  const handleExpandAll = useCallback(() => {
    onExpandAll()
    // Allow React to commit the new nodes, then fit view
    requestAnimationFrame(() => fitView({ duration: 300, padding: 0.1 }))
  }, [onExpandAll, fitView])

  const handleCollapseAll = useCallback(() => {
    onCollapseAll()
    requestAnimationFrame(() => fitView({ duration: 300, padding: 0.1 }))
  }, [onCollapseAll, fitView])

  const hasData = nodesWithCallbacks.length > 0

  const miniMapNodeColor = useCallback((node: Node) => {
    if (node.type === 'department') return 'hsl(var(--primary) / 0.15)'
    return 'hsl(var(--muted))'
  }, [])

  if (isLoading) {
    return (
      <div className="space-y-6">
        <PageHeader
          icon={GitBranch}
          title={t('hr.orgChart.title')}
          description={t('hr.orgChart.description')}
          responsive
        />
        <Card className="shadow-sm">
          <CardContent>
            <div className="space-y-3">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="flex items-center gap-3 p-3">
                  <Skeleton className="h-5 w-5" />
                  <Skeleton className="h-9 w-9 rounded-full" />
                  <div className="flex-1 space-y-1">
                    <Skeleton className="h-4 w-40" />
                    <Skeleton className="h-3 w-24" />
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={GitBranch}
        title={t('hr.orgChart.title')}
        description={t('hr.orgChart.description')}
        responsive
        action={
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              className="cursor-pointer"
              onClick={handleExpandAll}
              disabled={!hasData}
            >
              <Maximize2 className="mr-2 h-4 w-4" />
              {t('hr.orgChart.expandAll')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="cursor-pointer"
              onClick={handleCollapseAll}
              disabled={!hasData}
            >
              <Minimize2 className="mr-2 h-4 w-4" />
              {t('hr.orgChart.collapseAll')}
            </Button>
          </div>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardContent className="p-0">
          {!hasData ? (
            <EmptyState
              icon={Users}
              title={t('hr.orgChart.noData')}
              description={t('hr.orgChart.noDataDescription')}
              className="border-0 rounded-none px-4 py-12"
            />
          ) : (
            <div
              className={`w-full transition-opacity duration-300 ${isFetching || isFilterPending ? 'opacity-60' : 'opacity-100'}`}
              style={{ height: 'calc(100vh - 260px)', minHeight: '400px' }}
            >
              <ReactFlow
                nodes={nodesWithCallbacks}
                edges={edges}
                nodeTypes={nodeTypes}
                defaultEdgeOptions={defaultEdgeOptions}
                onNodesChange={handleNodesChange}
                onNodeDoubleClick={handleNodeDoubleClick}
                fitView
                fitViewOptions={{ padding: 0.15 }}
                minZoom={0.1}
                maxZoom={3}
                proOptions={{ hideAttribution: true }}
                nodesDraggable={false}
                nodesConnectable={false}
                elementsSelectable
              >
                <Background variant={BackgroundVariant.Dots} gap={20} size={1} />
                <Controls position="bottom-left" showInteractive={false} />
                {nodesWithCallbacks.length >= MINIMAP_THRESHOLD && (
                  <MiniMap
                    position="bottom-right"
                    nodeColor={miniMapNodeColor}
                    pannable
                    zoomable
                    maskColor="rgba(0,0,0,0.1)"
                  />
                )}

                {/* Search + filter overlay */}
                <Panel position="top-left">
                  <div className="flex flex-wrap items-center gap-2 rounded-lg border bg-card/95 p-2 shadow-md backdrop-blur-sm">
                    <div className="relative min-w-[200px] flex-1">
                      <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                      <Input
                        placeholder={t('hr.orgChart.search')}
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="h-9 pl-9"
                        aria-label={t('hr.orgChart.search')}
                      />
                    </div>
                    <Select value={departmentFilter} onValueChange={handleDepartmentFilterChange}>
                      <SelectTrigger
                        className="h-9 w-[200px] cursor-pointer"
                        aria-label={t('hr.filterByDepartment')}
                      >
                        <SelectValue placeholder={t('hr.filterByDepartment')} />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all" className="cursor-pointer">
                          {t('hr.allDepartments')}
                        </SelectItem>
                        {flatDepts.map((dept) => (
                          <SelectItem key={dept.id} value={dept.id} className="cursor-pointer">
                            {dept.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </Panel>
              </ReactFlow>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

export const OrgChartPage = () => (
  <ReactFlowProvider>
    <OrgChartCanvas />
  </ReactFlowProvider>
)

export default OrgChartPage
