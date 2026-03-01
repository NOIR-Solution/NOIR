/// <reference types="vite/client" />
/// <reference types="vite-plugin-pwa/react" />

declare module 'd3-org-chart' {
  export class OrgChart<TData = Record<string, unknown>> {
    constructor()
    container(selector: string | HTMLElement): this
    data(data: TData[]): this
    nodeWidth(fn: (d: unknown) => number): this
    nodeHeight(fn: (d: unknown) => number): this
    childrenMargin(fn: (d: unknown) => number): this
    compactMarginBetween(fn: (d: unknown) => number): this
    compactMarginPair(fn: (d: unknown) => number): this
    siblingsMargin(fn: (d: unknown) => number): this
    neighbourMargin(fn: (n1: unknown, n2: unknown) => number): this
    nodeId(fn: (d: TData) => string): this
    parentNodeId(fn: (d: TData) => string | null): this
    nodeContent(fn: (d: { id: string; data: TData; width: number; height: number; depth: number; _highlighted?: boolean; _upToTheRootHighlighted?: boolean; _expanded?: boolean }) => string): this
    buttonContent(fn: (opts: { node: unknown; state: unknown }) => string): this
    nodeUpdate(fn: (this: SVGGElement, d: unknown, i: number, arr: ArrayLike<SVGGElement>) => void): this
    linkUpdate(fn: (this: SVGPathElement, d: unknown, i: number, arr: ArrayLike<SVGPathElement>) => void): this
    onNodeClick(fn: (d: { data: TData }) => void): this
    layout(layout: 'top' | 'left' | 'right' | 'bottom'): this
    compact(compact: boolean): this
    initialExpandLevel(level: number): this
    duration(ms: number): this
    scaleExtent(extent: [number, number]): this
    svgHeight(height: number): this
    svgWidth(width: number): this
    setActiveNodeCentered(centered: boolean): this
    imageName(name: string): this
    render(): this
    expandAll(): this
    collapseAll(): this
    fit(opts?: { animate?: boolean; scale?: boolean; onCompleted?: () => void }): this
    zoomIn(): void
    zoomOut(): void
    setCentered(nodeId: string): this
    setHighlighted(nodeId: string): this
    setUpToTheRootHighlighted(nodeId: string): this
    clearHighlighting(): this
    setExpanded(id: string, expanded?: boolean): this
    exportImg(opts?: { full?: boolean; scale?: number; save?: boolean; backgroundColor?: string; onLoad?: (d: unknown) => unknown }): void
    exportSvg(): this
    getChartState(): Record<string, unknown>
    update(node: unknown): void
  }
}
