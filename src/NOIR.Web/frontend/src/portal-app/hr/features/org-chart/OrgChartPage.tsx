import { useState, useEffect, useRef, useMemo, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { OrgChart } from 'd3-org-chart'
import {
  GitBranch,
  Search,
  Download,
  Users,
  ZoomIn,
  ZoomOut,
  Maximize2,
  Minimize2,
  Maximize,
} from 'lucide-react'
import {
  Button,
  Card,
  CardContent,
  CardHeader,
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
import type { OrgChartNodeDto } from '@/types/hr'

/** Flat node shape expected by d3-org-chart */
interface FlatOrgNode {
  id: string
  parentId: string | null
  type: 'Department' | 'Employee'
  name: string
  subtitle: string | null
  avatarUrl: string | null
  employeeCount: number | null
  status: string | null
}

/** Recursively flatten nested OrgChartNodeDto tree into flat array with parentId */
const flattenTree = (
  nodes: OrgChartNodeDto[],
  parentId: string | null = null,
): FlatOrgNode[] => {
  const result: FlatOrgNode[] = []
  for (const node of nodes) {
    result.push({
      id: node.id,
      parentId,
      type: node.type,
      name: node.name,
      subtitle: node.subtitle ?? null,
      avatarUrl: node.avatarUrl ?? null,
      employeeCount: node.employeeCount ?? null,
      status: node.status ?? null,
    })
    if (node.children?.length) {
      result.push(...flattenTree(node.children, node.id))
    }
  }
  return result
}

/** Generate initials from a name string */
const getInitials = (name: string) =>
  name
    .split(' ')
    .map((n) => n[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()

/** Build the HTML string for a department card node */
const buildDepartmentCard = (d: { data: FlatOrgNode; width: number; height: number; _highlighted?: boolean }) => {
  const { name, subtitle, employeeCount } = d.data
  const highlighted = d._highlighted ? 'border-color: hsl(var(--primary)); box-shadow: 0 0 0 2px hsl(var(--primary) / 0.3);' : ''

  return `<div style="
    font-family: var(--font-sans, system-ui, sans-serif);
    width:${d.width}px;
    height:${d.height}px;
    display:flex;
    align-items:center;
    gap:12px;
    padding:16px;
    border-radius:12px;
    border:1px solid hsl(var(--border));
    background:hsl(var(--card));
    color:hsl(var(--card-foreground));
    box-shadow:0 1px 3px 0 rgb(0 0 0 / 0.1);
    transition:box-shadow 0.3s, border-color 0.3s;
    ${highlighted}
    cursor:pointer;
  ">
    <div style="
      flex-shrink:0;
      width:40px;
      height:40px;
      border-radius:50%;
      display:flex;
      align-items:center;
      justify-content:center;
      background:hsl(217 91% 60% / 0.1);
    ">
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="hsl(217 91% 60%)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <rect width="16" height="20" x="4" y="2" rx="2" ry="2"/>
        <path d="M9 22v-4h6v4"/>
        <path d="M8 6h.01"/>
        <path d="M16 6h.01"/>
        <path d="M12 6h.01"/>
        <path d="M12 10h.01"/>
        <path d="M12 14h.01"/>
        <path d="M16 10h.01"/>
        <path d="M16 14h.01"/>
        <path d="M8 10h.01"/>
        <path d="M8 14h.01"/>
      </svg>
    </div>
    <div style="flex:1;min-width:0;overflow:hidden;">
      <div style="display:flex;align-items:center;gap:8px;">
        <span style="font-weight:600;font-size:14px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">${name}</span>
        ${employeeCount != null ? `<span style="
          font-size:11px;
          padding:1px 8px;
          border-radius:9999px;
          border:1px solid hsl(var(--border));
          white-space:nowrap;
          color:hsl(var(--muted-foreground));
        ">${employeeCount}</span>` : ''}
      </div>
      ${subtitle ? `<div style="font-size:12px;color:hsl(var(--muted-foreground));white-space:nowrap;overflow:hidden;text-overflow:ellipsis;margin-top:2px;">${subtitle}</div>` : ''}
    </div>
  </div>`
}

/** Status color mapping */
const STATUS_COLORS: Record<string, { bg: string; text: string; border: string }> = {
  active: { bg: 'hsl(142 76% 36% / 0.1)', text: 'hsl(142 76% 36%)', border: 'hsl(142 76% 36% / 0.3)' },
  suspended: { bg: 'hsl(48 96% 53% / 0.1)', text: 'hsl(48 96% 40%)', border: 'hsl(48 96% 53% / 0.3)' },
  resigned: { bg: 'hsl(220 9% 46% / 0.1)', text: 'hsl(220 9% 46%)', border: 'hsl(220 9% 46% / 0.3)' },
  terminated: { bg: 'hsl(0 84% 60% / 0.1)', text: 'hsl(0 84% 60%)', border: 'hsl(0 84% 60% / 0.3)' },
}

/** Build the HTML string for an employee card node */
const buildEmployeeCard = (d: { data: FlatOrgNode; width: number; height: number; _highlighted?: boolean }) => {
  const { name, subtitle, avatarUrl, status } = d.data
  const highlighted = d._highlighted ? 'border-color: hsl(var(--primary)); box-shadow: 0 0 0 2px hsl(var(--primary) / 0.3);' : ''
  const initials = getInitials(name)
  const statusKey = (status ?? '').toLowerCase()
  const colors = STATUS_COLORS[statusKey] ?? STATUS_COLORS['resigned']

  const avatarHtml = avatarUrl
    ? `<img src="${avatarUrl}" alt="${name}" style="width:40px;height:40px;border-radius:50%;object-fit:cover;" />`
    : `<div style="
        width:40px;
        height:40px;
        border-radius:50%;
        display:flex;
        align-items:center;
        justify-content:center;
        background:hsl(var(--primary) / 0.1);
        color:hsl(var(--primary));
        font-size:12px;
        font-weight:600;
      ">${initials}</div>`

  const statusBadge = status
    ? `<span style="
        font-size:10px;
        padding:1px 6px;
        border-radius:9999px;
        border:1px solid ${colors.border};
        background:${colors.bg};
        color:${colors.text};
        white-space:nowrap;
      ">${status}</span>`
    : ''

  return `<div style="
    font-family: var(--font-sans, system-ui, sans-serif);
    width:${d.width}px;
    height:${d.height}px;
    display:flex;
    align-items:center;
    gap:12px;
    padding:16px;
    border-radius:12px;
    border:1px solid hsl(var(--border));
    background:hsl(var(--card));
    color:hsl(var(--card-foreground));
    box-shadow:0 1px 3px 0 rgb(0 0 0 / 0.1);
    transition:box-shadow 0.3s, border-color 0.3s;
    ${highlighted}
    cursor:pointer;
  ">
    <div style="flex-shrink:0;">${avatarHtml}</div>
    <div style="flex:1;min-width:0;overflow:hidden;">
      <div style="display:flex;align-items:center;gap:8px;">
        <span style="font-weight:600;font-size:14px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">${name}</span>
        ${statusBadge}
      </div>
      ${subtitle ? `<div style="font-size:12px;color:hsl(var(--muted-foreground));white-space:nowrap;overflow:hidden;text-overflow:ellipsis;margin-top:2px;">${subtitle}</div>` : ''}
    </div>
  </div>`
}

export const OrgChartPage = () => {
  const { t } = useTranslation('common')
  const [searchInput, setSearchInput] = useState('')
  const [departmentFilter, setDepartmentFilter] = useState<string>('all')
  const chartContainerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<OrgChart<FlatOrgNode> | null>(null)

  const deptId = departmentFilter !== 'all' ? departmentFilter : undefined
  const { data: orgChartData, isLoading } = useOrgChartQuery(deptId)
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

  const flatData = useMemo(() => {
    if (!orgChartData?.length) return []
    const flat = flattenTree(orgChartData)
    // If there are multiple root nodes, create a synthetic root
    const roots = flat.filter((n) => n.parentId === null)
    if (roots.length > 1) {
      flat.unshift({
        id: '__root__',
        parentId: null,
        type: 'Department',
        name: t('hr.orgChart.title'),
        subtitle: null,
        avatarUrl: null,
        employeeCount: null,
        status: null,
      })
      for (const root of roots) {
        root.parentId = '__root__'
      }
    }
    return flat
  }, [orgChartData, t])

  // Initialize / update the chart whenever flatData changes
  useEffect(() => {
    if (!chartContainerRef.current || flatData.length === 0) return

    // Clear any existing content
    chartContainerRef.current.innerHTML = ''

    const chart = new OrgChart<FlatOrgNode>()
      .container(chartContainerRef.current)
      .data(flatData)
      .nodeId((d) => d.id)
      .parentNodeId((d) => d.parentId)
      .nodeWidth(() => 280)
      .nodeHeight(() => 80)
      .childrenMargin(() => 50)
      .compactMarginBetween(() => 25)
      .compactMarginPair(() => 80)
      .siblingsMargin(() => 30)
      .neighbourMargin(() => 50)
      .initialExpandLevel(2)
      .compact(true)
      .layout('top')
      .duration(300)
      .scaleExtent([0.1, 3])
      .setActiveNodeCentered(true)
      .imageName('org-chart')
      .nodeContent((d) => {
        if (d.data.type === 'Department') {
          return buildDepartmentCard(d)
        }
        return buildEmployeeCard(d)
      })
      .buttonContent(({ node }) => {
        const children = (node as { data: { _directSubordinatesPaging?: number } }).data._directSubordinatesPaging ?? 0
        return `<div style="
          border:1px solid hsl(var(--border));
          border-radius:6px;
          padding:2px 6px;
          font-size:11px;
          background:hsl(var(--card));
          color:hsl(var(--muted-foreground));
          cursor:pointer;
          display:flex;
          align-items:center;
          gap:2px;
        ">${children}</div>`
      })
      .nodeUpdate(function (_d, _i, _arr) {
        // Remove default node rectangle styling
        const el = this as SVGGElement
        const rect = el.querySelector('.node-rect') as SVGRectElement | null
        if (rect) {
          rect.setAttribute('stroke', 'none')
          rect.setAttribute('fill', 'none')
        }
      })
      .linkUpdate(function (d) {
        const el = this as SVGPathElement
        const data = d as { data?: { _upToTheRootHighlighted?: boolean } }
        const isHighlighted = data?.data?._upToTheRootHighlighted
        el.setAttribute('stroke', isHighlighted ? 'hsl(var(--primary))' : 'hsl(var(--border))')
        el.setAttribute('stroke-width', isHighlighted ? '3' : '1.5')
      })
      .render()

    chartRef.current = chart

    // Fit after initial render
    requestAnimationFrame(() => {
      chart.fit({ animate: true })
    })

    return () => {
      chartRef.current = null
    }
  }, [flatData])

  // Handle search: highlight + center matching node
  const handleSearch = useCallback(
    (term: string) => {
      setSearchInput(term)
      const chart = chartRef.current
      if (!chart) return

      chart.clearHighlighting()
      if (!term.trim()) {
        chart.render()
        return
      }

      const lower = term.toLowerCase()
      const match = flatData.find(
        (n) =>
          n.name.toLowerCase().includes(lower) ||
          n.subtitle?.toLowerCase().includes(lower),
      )

      if (match) {
        chart.clearHighlighting()
        chart.setUpToTheRootHighlighted(match.id).render().fit()
      }
    },
    [flatData],
  )

  const handleZoomIn = useCallback(() => chartRef.current?.zoomIn(), [])
  const handleZoomOut = useCallback(() => chartRef.current?.zoomOut(), [])
  const handleFit = useCallback(() => chartRef.current?.fit({ animate: true }), [])

  const handleExpandAll = useCallback(() => {
    chartRef.current?.expandAll().fit({ animate: true })
  }, [])

  const handleCollapseAll = useCallback(() => {
    chartRef.current?.collapseAll().fit({ animate: true })
  }, [])

  const handleExport = useCallback(() => {
    chartRef.current?.exportImg({ full: true, scale: 3, save: true, backgroundColor: '#FAFAFA' })
  }, [])

  const hasData = flatData.length > 0

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
              <Maximize2 className="h-4 w-4 mr-2" />
              {t('hr.orgChart.expandAll')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="cursor-pointer"
              onClick={handleCollapseAll}
              disabled={!hasData}
            >
              <Minimize2 className="h-4 w-4 mr-2" />
              {t('hr.orgChart.collapseAll')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="cursor-pointer"
              onClick={handleExport}
              disabled={!hasData}
            >
              <Download className="h-4 w-4 mr-2" />
              {t('hr.orgChart.export')}
            </Button>
          </div>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="flex flex-wrap items-center gap-2">
            <div className="relative flex-1 min-w-[200px]">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder={t('hr.orgChart.search')}
                value={searchInput}
                onChange={(e) => handleSearch(e.target.value)}
                className="pl-9 h-9"
                aria-label={t('hr.orgChart.search')}
              />
            </div>
            <Select value={departmentFilter} onValueChange={setDepartmentFilter}>
              <SelectTrigger className="w-[200px] h-9 cursor-pointer" aria-label={t('hr.filterByDepartment')}>
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
            <div className="flex items-center gap-1">
              <Button
                variant="outline"
                size="icon"
                className="h-9 w-9 cursor-pointer"
                onClick={handleZoomIn}
                disabled={!hasData}
                aria-label={t('hr.orgChart.zoomIn')}
              >
                <ZoomIn className="h-4 w-4" />
              </Button>
              <Button
                variant="outline"
                size="icon"
                className="h-9 w-9 cursor-pointer"
                onClick={handleZoomOut}
                disabled={!hasData}
                aria-label={t('hr.orgChart.zoomOut')}
              >
                <ZoomOut className="h-4 w-4" />
              </Button>
              <Button
                variant="outline"
                size="icon"
                className="h-9 w-9 cursor-pointer"
                onClick={handleFit}
                disabled={!hasData}
                aria-label={t('hr.orgChart.fitScreen')}
              >
                <Maximize className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="flex items-center gap-3 p-3">
                  <Skeleton className="h-5 w-5" />
                  <Skeleton className="h-9 w-9 rounded-full" />
                  <div className="space-y-1 flex-1">
                    <Skeleton className="h-4 w-40" />
                    <Skeleton className="h-3 w-24" />
                  </div>
                </div>
              ))}
            </div>
          ) : !hasData ? (
            <EmptyState
              icon={Users}
              title={t('hr.orgChart.noData')}
              description={t('hr.orgChart.noDataDescription')}
              className="border-0 rounded-none px-4 py-12"
            />
          ) : (
            <div
              ref={chartContainerRef}
              className="w-full"
              style={{ height: 'calc(100vh - 340px)', minHeight: '400px' }}
            />
          )}
        </CardContent>
      </Card>
    </div>
  )
}

export default OrgChartPage
