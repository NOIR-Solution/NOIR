# Workflow: HR Org Chart — React Flow Migration

> Generated: 2026-03-21 | Status: READY FOR IMPLEMENTATION
> Design source: Brainstorm + Design session (same day)

## Overview

Replace `d3-org-chart` (HTML strings in SVG) with `@xyflow/react` + `@dagrejs/dagre` (real React components). Add employee-level reporting lines using existing `ManagerId`. Compact cards with NodeToolbar on select, double-click to navigate.

**Scope:** 1 backend handler rewrite + 1 new spec + 1 DTO change + 6 frontend files (rewrite/create) + cleanup

---

## Phase 1: Backend — Flat DTO + Employee Nodes

**Goal:** Change the org chart API to return a flat list of departments AND employees with `ParentId` + `ManagerId` fields.

### Task 1.1: Update `OrgChartNodeDto` (DTO change)

**File:** `src/NOIR.Application/Features/Hr/DTOs/HrDtos.cs`

**Action:** Replace nested DTO with flat DTO:
```csharp
// REMOVE:
// public sealed record OrgChartNodeDto(
//     Guid Id, OrgChartNodeType Type, string Name, string? Subtitle,
//     string? AvatarUrl, int? EmployeeCount, EmployeeStatus? Status,
//     IReadOnlyList<OrgChartNodeDto> Children);

// ADD:
public sealed record OrgChartNodeDto(
    Guid Id,
    OrgChartNodeType Type,
    string Name,
    string? Subtitle,
    string? AvatarUrl,
    int? EmployeeCount,
    EmployeeStatus? Status,
    Guid? ParentId,
    Guid? ManagerId);
```

**Verify:** `dotnet build src/NOIR.sln` — will show compile errors in handler (expected, fixed in 1.3).

### Task 1.2: Create `OrgChartEmployeesSpec`

**File:** `src/NOIR.Application/Features/Hr/Specifications/EmployeeSpecs.cs` (add to existing file)

**Action:** Add new spec class:
```csharp
public sealed class OrgChartEmployeesSpec : Specification<Employee>
{
    public OrgChartEmployeesSpec(IEnumerable<Guid> departmentIds)
    {
        var deptIdList = departmentIds.ToList();
        Query.Where(e => deptIdList.Contains(e.DepartmentId))
             .AsNoTracking()
             .TagWith("OrgChartEmployees");
    }
}
```

### Task 1.3: Rewrite `GetOrgChartQueryHandler`

**File:** `src/NOIR.Application/Features/Hr/Queries/GetOrgChart/GetOrgChartQueryHandler.cs`

**Action:** Full rewrite — return flat list with both departments and employees.

**Logic:**
1. Load all active departments (existing `AllDepartmentsSpec`)
2. If `departmentId` filter → collect subtree IDs recursively; else → all dept IDs
3. Load employees for target departments (`OrgChartEmployeesSpec`)
4. Build flat `List<OrgChartNodeDto>`:
   - Department nodes: `ParentId = ParentDepartmentId` (null if root or filtered parent outside set)
   - Employee nodes: `ParentId = DepartmentId`, `ManagerId = employee.ManagerId` (only if manager is also in result set)
5. Return flat list

**Edge case:** When filtering by department, a manager in another department won't be in the result set. Set `ManagerId = null` for those employees to avoid dangling edge references.

### Task 1.4: Check MCP tools

**Action:** `grep -r "GetOrgChartQuery\|OrgChartNodeDto" src/NOIR.Web/Mcp/` — already confirmed no MCP tools reference org chart. No MCP changes needed.

### Checkpoint 1

```bash
dotnet build src/NOIR.sln        # Must pass (0 errors)
dotnet test src/NOIR.sln         # Must pass (all green)
```

---

## Phase 2: Frontend Dependencies

**Goal:** Swap libraries in `package.json`.

### Task 2.1: Install React Flow + dagre, remove d3-org-chart

**Commands:**
```bash
cd src/NOIR.Web/frontend
pnpm remove d3-org-chart
pnpm add @xyflow/react @dagrejs/dagre
pnpm add -D @types/dagre          # dagre types (if not bundled)
```

### Task 2.2: Remove d3-org-chart type declarations

**File:** `src/NOIR.Web/frontend/src/vite-env.d.ts`

**Action:** Remove the entire `declare module 'd3-org-chart' { ... }` block (lines 4–48). Keep the two `/// <reference>` lines at the top.

### Checkpoint 2

```bash
cd src/NOIR.Web/frontend && pnpm run build   # Must pass (org chart page will error — that's expected, it still imports d3-org-chart)
```

Note: Build may fail at this point because `OrgChartPage.tsx` still imports `d3-org-chart`. That's OK — Phase 3–6 will rewrite it.

---

## Phase 3: Frontend Types + Helpers

**Goal:** Create the transformation layer between API data and React Flow.

### Task 3.1: Update `OrgChartNodeDto` type

**File:** `src/NOIR.Web/frontend/src/types/hr.ts`

**Action:** Replace:
```typescript
// REMOVE:
// export interface OrgChartNodeDto {
//   id: string
//   type: OrgChartNodeType
//   name: string
//   subtitle?: string | null
//   avatarUrl?: string | null
//   employeeCount?: number | null
//   status?: EmployeeStatus | null
//   children: OrgChartNodeDto[]
// }

// ADD:
export interface OrgChartNodeDto {
  id: string
  type: OrgChartNodeType
  name: string
  subtitle?: string | null
  avatarUrl?: string | null
  employeeCount?: number | null
  status?: EmployeeStatus | null
  parentId?: string | null
  managerId?: string | null
}
```

### Task 3.2: Create `orgChartTypes.ts`

**File:** `src/NOIR.Web/frontend/src/portal-app/hr/features/org-chart/orgChartTypes.ts`

**Contents:**
- `DepartmentNodeData` interface (name, subtitle, employeeCount, childCount, expanded, highlighted, onToggle)
- `EmployeeNodeData` interface (name, subtitle, avatarUrl, status, directReportCount, expanded, highlighted, onToggle)
- Type aliases for edges: `HIERARCHY_EDGE` and `REPORTING_EDGE` constants
- Status color map: `statusToColorMap` mapping EmployeeStatus → getStatusBadgeClasses color key

### Task 3.3: Create `orgChartHelpers.ts`

**File:** `src/NOIR.Web/frontend/src/portal-app/hr/features/org-chart/orgChartHelpers.ts`

**Exports:**
1. `transformToReactFlow(apiNodes: OrgChartNodeDto[])` → `{ nodes: Node[], hierarchyEdges: Edge[], reportingEdges: Edge[] }`
   - Maps each apiNode → React Flow Node with `type: 'department' | 'employee'`
   - Builds hierarchy edges from `parentId` (solid, border color)
   - Builds reporting edges from `managerId` (dashed, primary color)
   - Computes `childCount` and `directReportCount` from the flat list

2. `getDescendantIds(nodeId: string, hierarchyEdges: Edge[])` → `Set<string>`
   - Recursive BFS/DFS to find all descendants via hierarchy edges
   - Used by expand/collapse

3. `getAncestorIds(nodeId: string, nodes: OrgChartNodeDto[])` → `string[]`
   - Walk up parentId chain to root
   - Used by search (ensure path to match is expanded)

### Checkpoint 3

No build check yet — these are standalone modules with no consumers.

---

## Phase 4: Layout Hook

**Goal:** Create the dagre layout hook with expand/collapse and search.

### Task 4.1: Create `useOrgChartLayout.ts`

**File:** `src/NOIR.Web/frontend/src/portal-app/hr/features/org-chart/useOrgChartLayout.ts`

**Interface:**
```typescript
interface UseOrgChartLayoutOptions {
  apiNodes: OrgChartNodeDto[]
  direction?: 'TB' | 'LR'
  initialExpandLevel?: number
}

interface UseOrgChartLayoutReturn {
  nodes: Node[]
  edges: Edge[]
  onToggleNode: (nodeId: string) => void
  onExpandAll: () => void
  onCollapseAll: () => void
  searchTerm: string
  setSearchTerm: (term: string) => void
  highlightedIds: Set<string>
}
```

**Internal logic:**
1. `transformToReactFlow()` on apiNodes → rfNodes, hierarchyEdges, reportingEdges
2. `expandedMap` state (Map<string, boolean>) — initialized from depth calculation
3. `applyVisibility()` — sets `hidden` on nodes/edges based on expandedMap
4. `applyDagreLayout()` — runs dagre on visible nodes + hierarchy edges only
5. Search: debounced term → find matches → expand ancestor paths → highlight
6. Returns positioned nodes + all visible edges (hierarchy + reporting)

**Dagre config:**
```typescript
const g = new dagre.graphlib.Graph().setDefaultEdgeLabel(() => ({}))
g.setGraph({ rankdir: direction, ranksep: 60, nodesep: 30 })
// Set each visible node: g.setNode(id, { width: 280, height: 80 })
// Set each hierarchy edge: g.setEdge(source, target)
dagre.layout(g)
// Correct positions: x -= 140, y -= 40 (center → top-left)
```

### Checkpoint 4

No build check yet — hook has no consumer.

---

## Phase 5: Custom Node Components

**Goal:** Create the two React Flow custom node components.

### Task 5.1: Create `DepartmentNode.tsx`

**File:** `src/NOIR.Web/frontend/src/portal-app/hr/features/org-chart/DepartmentNode.tsx`

**Structure:**
- `memo()` wrapped
- `Handle` target (top) + source (bottom)
- `NodeToolbar` on selected → "View Departments" button
- Building2 icon in primary/10 circle
- Name (bold, truncated) + Badge for employeeCount
- Subtitle (muted, truncated) — manager name
- Expand toggle button (`nodrag nopan` class) showing childCount

### Task 5.2: Create `EmployeeNode.tsx`

**File:** `src/NOIR.Web/frontend/src/portal-app/hr/features/org-chart/EmployeeNode.tsx`

**Structure:**
- `memo()` wrapped
- `Handle` target (top) + source (bottom)
- `NodeToolbar` on selected → "View Profile" button
- UIKit `<Avatar>` (src + fallback from name)
- Name (bold, truncated) + Badge with `getStatusBadgeClasses()`
- Subtitle (muted, truncated) — position/role
- Expand toggle button if directReportCount > 0

**Shared styling (both nodes):**
- `w-[280px] rounded-xl border bg-card p-4 shadow-sm hover:shadow-lg transition-all duration-300`
- Selected: `ring-2 ring-primary`
- Highlighted: `ring-2 ring-primary ring-offset-2`
- Dark mode: automatic via Tailwind + CSS vars

### Checkpoint 5

No build check yet — components not imported anywhere.

---

## Phase 6: Page Rewrite

**Goal:** Replace the entire `OrgChartPage.tsx` with React Flow implementation.

### Task 6.1: Rewrite `OrgChartPage.tsx`

**File:** `src/NOIR.Web/frontend/src/portal-app/hr/features/org-chart/OrgChartPage.tsx`

**Structure:**
```
OrgChartPage (outer)
├── PageHeader
│   └── action: [Expand All] [Collapse All]
│
└── Card
    └── CardContent (p-0 for full-bleed canvas)
        └── ReactFlowProvider
            └── OrgChartCanvas (inner component)
```

`OrgChartCanvas` (inner, uses `useReactFlow`):
```
useOrgChartQuery(deptId)
useOrgChartLayout({ apiNodes, direction: 'TB', initialExpandLevel: 2 })

<ReactFlow
  nodes, edges, nodeTypes, defaultEdgeOptions
  onNodeClick → select
  onNodeDoubleClick → navigate (employees → /portal/hr/employees/:id)
  onlyRenderVisibleElements
  fitView, fitViewOptions={{ padding: 0.15 }}
  minZoom={0.1}, maxZoom={3}
  proOptions={{ hideAttribution: true }}
>
  <Background variant="dots" />
  <Controls position="bottom-left" />
  <MiniMap position="bottom-right" nodeColor={colorFn} pannable zoomable />
  <Panel position="top-left">
    <search input + dept filter dropdown>
  </Panel>
</ReactFlow>
```

**nodeTypes** — defined OUTSIDE component:
```typescript
const nodeTypes = {
  department: DepartmentNode,
  employee: EmployeeNode,
}
```

**Event handlers:**
- `onNodeClick`: set selectedNodeId (React Flow handles selection styling)
- `onNodeDoubleClick`: `navigate(`/portal/hr/employees/${node.id}`)` for employees; no-op for departments (no detail page)

### Task 6.2: Update `useOrgChartQuery` (if needed)

**File:** `src/NOIR.Web/frontend/src/portal-app/hr/queries/useHrQueries.ts`

**Check:** The query hook likely expects the old nested shape. Update the return type to match the new flat `OrgChartNodeDto[]`. The `queryFn` (API call) stays the same — the backend returns the new shape.

### Checkpoint 6 (CRITICAL)

```bash
cd src/NOIR.Web/frontend && pnpm run build    # Must pass (0 errors, 0 warnings)
dotnet build src/NOIR.sln                      # Must pass
```

---

## Phase 7: Cleanup

**Goal:** Remove old code, update i18n, update tests.

### Task 7.1: Delete `exportUtils.ts`

**File:** `src/NOIR.Web/frontend/src/portal-app/hr/features/org-chart/exportUtils.ts`

**Action:** Delete file. No other files import it (only the old OrgChartPage used it).

### Task 7.2: Update i18n keys

**Files:** `public/locales/en/common.json`, `public/locales/vi/common.json`

**Action:**
- Remove: `export`, `zoomIn`, `zoomOut`, `fitScreen` (Controls component replaces custom buttons)
- Add: `viewProfile`, `viewDepartments`, `directReports`, `reportsTo`

### Task 7.3: Update e2e tests

**Files:**
- `e2e/tests/hr/org-chart.spec.ts` — update selectors (no more SVG/foreignObject, now React Flow DOM)
- `e2e/tests/hr/departments.spec.ts` — update org chart wait logic (line 272)
- `e2e/tests/ecommerce/data-linking.spec.ts` — update org chart assertions (lines 342-350)

**Key changes:** Replace `d3-org-chart` SVG selectors with React Flow class selectors (`.react-flow__node`, `.react-flow__edge`).

### Task 7.4: Clean up `vite-env.d.ts`

**Action:** Verify d3-org-chart declarations already removed in Phase 2. If any remnants, clean up.

### Checkpoint 7 (FINAL)

```bash
dotnet build src/NOIR.sln                                  # 0 errors
dotnet test src/NOIR.sln                                   # All pass
cd src/NOIR.Web/frontend && pnpm run build                 # 0 errors, 0 warnings
cd src/NOIR.Web/frontend && pnpm build-storybook           # 0 errors (no storybook changes expected)
```

---

## Phase 8: Visual Verification

**Goal:** Manual check that the org chart renders correctly.

### Task 8.1: Start dev server and verify

```bash
cd src/NOIR.Web && dotnet run                              # Backend
cd src/NOIR.Web/frontend && pnpm run dev                   # Frontend
```

**Checklist:**
- [ ] Navigate to `/portal/hr/org-chart`
- [ ] Departments render as nodes with building icon
- [ ] Employees render as nodes with avatars
- [ ] Hierarchy edges (solid) connect dept→subdept and dept→employee
- [ ] Reporting edges (dashed) connect manager→direct report
- [ ] MiniMap visible (bottom-right)
- [ ] Controls visible (bottom-left) — zoom in/out/fit
- [ ] Background dots visible
- [ ] Search input in top-left panel — type a name → node highlights
- [ ] Department filter dropdown works
- [ ] Expand/Collapse All buttons in header work
- [ ] Per-node expand toggle works
- [ ] Single click → NodeToolbar appears (View Profile / View Departments)
- [ ] Double click employee → navigates to `/portal/hr/employees/:id`
- [ ] Dark mode: toggle theme → all nodes/edges/minimap adapt
- [ ] Vietnamese: switch language → all labels in Vietnamese

---

## Dependency Graph

```
Phase 1 (Backend)
    │
    ├── Task 1.1 (DTO) ──→ Task 1.3 (Handler)
    ├── Task 1.2 (Spec) ──→ Task 1.3 (Handler)
    └── Task 1.4 (MCP check) — independent

Phase 2 (Dependencies) — independent of Phase 1
    │
    ├── Task 2.1 (pnpm add/remove)
    └── Task 2.2 (vite-env.d.ts)

Phase 3 (Types + Helpers) — depends on Phase 2
    │
    ├── Task 3.1 (DTO type)
    ├── Task 3.2 (orgChartTypes)
    └── Task 3.3 (orgChartHelpers) — depends on 3.1, 3.2

Phase 4 (Layout Hook) — depends on Phase 3
    └── Task 4.1 (useOrgChartLayout)

Phase 5 (Nodes) — depends on Phase 3 (parallel with Phase 4)
    ├── Task 5.1 (DepartmentNode)
    └── Task 5.2 (EmployeeNode)

Phase 6 (Page) — depends on Phase 1 + 4 + 5
    ├── Task 6.1 (OrgChartPage rewrite)
    └── Task 6.2 (query hook update)

Phase 7 (Cleanup) — depends on Phase 6
    ├── Task 7.1 (delete exportUtils)
    ├── Task 7.2 (i18n) — independent
    ├── Task 7.3 (e2e tests) — independent
    └── Task 7.4 (vite-env verify)

Phase 8 (Verify) — depends on Phase 7
    └── Task 8.1 (visual check)
```

**Parallelizable:** Phase 4 + Phase 5 can run in parallel. Tasks 7.1–7.4 can all run in parallel.

---

## Risk Register

| Risk | Mitigation |
|------|-----------|
| dagre layout doesn't handle disconnected components well (multiple root depts) | dagre places disconnected subgraphs side by side — test with multi-root org |
| Cross-department reporting edges overlap hierarchy edges | Dashed style + primary color distinguishes them; edges use single handle |
| Large org (500+ employees) performance | `onlyRenderVisibleElements` + `React.memo` on nodes + expand/collapse hides nodes |
| Reporting edge crosses many ranks (visually messy) | smoothstep routing handles this; add toggle to show/hide reporting edges if needed |
| @xyflow/react v12 breaking changes vs docs | Pin exact version in package.json |

---

## Files Changed Summary

| Action | File | Lines (est.) |
|--------|------|-------------|
| MODIFY | `Application/Features/Hr/DTOs/HrDtos.cs` | ~5 |
| MODIFY | `Application/Features/Hr/Specifications/EmployeeSpecs.cs` | ~12 |
| REWRITE | `Application/Features/Hr/Queries/GetOrgChart/GetOrgChartQueryHandler.cs` | ~80 |
| MODIFY | `frontend/package.json` | ~3 |
| MODIFY | `frontend/src/vite-env.d.ts` | -45 |
| MODIFY | `frontend/src/types/hr.ts` | ~5 |
| CREATE | `frontend/.../org-chart/orgChartTypes.ts` | ~40 |
| CREATE | `frontend/.../org-chart/orgChartHelpers.ts` | ~100 |
| CREATE | `frontend/.../org-chart/useOrgChartLayout.ts` | ~150 |
| CREATE | `frontend/.../org-chart/DepartmentNode.tsx` | ~80 |
| CREATE | `frontend/.../org-chart/EmployeeNode.tsx` | ~90 |
| REWRITE | `frontend/.../org-chart/OrgChartPage.tsx` | ~200 |
| DELETE | `frontend/.../org-chart/exportUtils.ts` | -48 |
| MODIFY | `frontend/public/locales/en/common.json` | ~6 |
| MODIFY | `frontend/public/locales/vi/common.json` | ~6 |
| MODIFY | `frontend/e2e/tests/hr/org-chart.spec.ts` | ~20 |
| MODIFY | `frontend/e2e/tests/hr/departments.spec.ts` | ~5 |
| MODIFY | `frontend/e2e/tests/ecommerce/data-linking.spec.ts` | ~10 |
| **Total** | **18 files** | **~900 lines** |
