/**
 * AuditDashboard - Real-time audit monitoring dashboard
 *
 * Features:
 * - Real-time stats cards with live counters
 * - Event timeline chart
 * - Live event stream
 * - Full-text search
 */
import { useState, useEffect, useMemo } from 'react'
import {
  Activity,
  Clock,
  Database,
  FileText,
  Globe,
  Search,
  Settings,
  TrendingUp,
  User,
  UserX,
  Zap,
  RefreshCw,
  Wifi,
  WifiOff,
} from 'lucide-react'
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { useAuditStream, type ConnectionState } from '@/hooks/useAuditStream'
import { searchAuditLogs, getDetailedAuditStats, getAuditStats } from '@/services/audit'
import type { UnifiedAuditEvent, AuditSearchResult, AuditDetailedStats, AuditStatsUpdate, AuditEventType } from '@/types'

const CHART_COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4']

function formatNumber(num: number): string {
  if (num >= 1000000) return `${(num / 1000000).toFixed(1)}M`
  if (num >= 1000) return `${(num / 1000).toFixed(1)}K`
  return num.toString()
}

function formatTimestamp(timestamp: string): string {
  const date = new Date(timestamp)
  return date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

function getEventTypeColor(eventType: string): string {
  switch (eventType) {
    case 'HttpRequest': return 'bg-blue-500/10 text-blue-500 border-blue-500/20'
    case 'Handler': return 'bg-purple-500/10 text-purple-500 border-purple-500/20'
    case 'EntityChange':
    case 'Entity': return 'bg-green-500/10 text-green-500 border-green-500/20'
    default: return 'bg-gray-500/10 text-gray-500 border-gray-500/20'
  }
}

function ConnectionStatus({ state }: { state: ConnectionState }) {
  const config = {
    disconnected: { icon: WifiOff, text: 'Disconnected', className: 'text-gray-500' },
    connecting: { icon: Wifi, text: 'Connecting...', className: 'text-yellow-500 animate-pulse' },
    connected: { icon: Wifi, text: 'Live', className: 'text-green-500' },
    reconnecting: { icon: RefreshCw, text: 'Reconnecting...', className: 'text-yellow-500 animate-spin' },
    error: { icon: WifiOff, text: 'Error', className: 'text-red-500' },
  }[state]

  const Icon = config.icon

  return (
    <div className={`flex items-center gap-2 ${config.className}`}>
      <Icon className="h-4 w-4" />
      <span className="text-sm font-medium">{config.text}</span>
    </div>
  )
}

interface StatCardProps {
  title: string
  value: number
  subtitle: string
  icon: React.ComponentType<{ className?: string }>
  trend?: number
}

function StatCard({ title, value, subtitle, icon: Icon, trend }: StatCardProps) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{formatNumber(value)}</div>
        <p className="text-xs text-muted-foreground flex items-center gap-1">
          {trend !== undefined && (
            <TrendingUp className={`h-3 w-3 ${trend >= 0 ? 'text-green-500' : 'text-red-500 rotate-180'}`} />
          )}
          {subtitle}
        </p>
      </CardContent>
    </Card>
  )
}

function EventStreamItem({ event }: { event: UnifiedAuditEvent }) {
  // Extract URL path for HttpRequest events
  const getDisplayTitle = () => {
    if (event.eventType === 'HttpRequest') {
      const urlStr = event.details?.url
      if (typeof urlStr === 'string') {
        try {
          const url = new URL(urlStr, window.location.origin)
          return url.pathname
        } catch {
          return urlStr
        }
      }
    }
    if (event.eventType === 'Handler') {
      return event.entityType // Handler name
    }
    // Entity changes: show entity type + operation
    return event.entityType
  }

  const getStatusInfo = () => {
    if (event.eventType === 'HttpRequest') {
      const status = event.details?.statusCode
      const duration = event.details?.durationMs
      const parts: string[] = []
      if (typeof status === 'number') {
        parts.push(status >= 400 ? `❌ ${status}` : `✓ ${status}`)
      }
      if (typeof duration === 'number') {
        parts.push(`${duration}ms`)
      }
      return parts.join(' · ')
    }
    if (event.eventType === 'Handler') {
      const isSuccess = event.details?.isSuccess
      return isSuccess === false ? '❌ Failed' : '✓ Success'
    }
    // Entity: show operation
    if (event.operation) return event.operation
    return ''
  }

  // Get user display info
  const isAuthenticated = Boolean(event.userName || event.userId)
  const userDisplay = event.userName || (event.userId ? `User ${event.userId.slice(0, 8)}...` : null)
  const statusInfo = getStatusInfo()

  return (
    <div className="flex items-start gap-3 py-2 px-3 hover:bg-muted/50 rounded-lg transition-colors">
      <div className="flex-shrink-0 mt-1">
        <Badge variant="outline" className={`text-xs ${getEventTypeColor(event.eventType)}`}>
          {event.eventType === 'HttpRequest' ? event.operation : event.eventType}
        </Badge>
      </div>
      <div className="flex-1 min-w-0">
        <div className="font-medium text-sm truncate">
          {getDisplayTitle()}
        </div>
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          {/* User info with icon */}
          <span className={`flex items-center gap-1 ${isAuthenticated ? 'text-blue-500' : 'text-gray-400'}`}>
            {isAuthenticated ? (
              <User className="h-3 w-3" />
            ) : (
              <UserX className="h-3 w-3" />
            )}
            <span className="truncate max-w-[120px]">
              {userDisplay || 'Anonymous'}
            </span>
          </span>
          {/* Status info */}
          {statusInfo && (
            <>
              <span className="text-muted-foreground/50">·</span>
              <span className="truncate">{statusInfo}</span>
            </>
          )}
        </div>
      </div>
      <div className="flex-shrink-0 text-xs text-muted-foreground">
        {formatTimestamp(event.timestamp)}
      </div>
    </div>
  )
}

export function AuditDashboard() {
  const {
    connectionState,
    stats: signalRStats,
    recentEvents,
    subscribeToAllEvents,
  } = useAuditStream({ autoConnect: true, autoDashboard: true })

  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<AuditSearchResult | null>(null)
  const [isSearching, setIsSearching] = useState(false)
  const [detailedStats, setDetailedStats] = useState<AuditDetailedStats | null>(null)
  const [restStats, setRestStats] = useState<AuditStatsUpdate | null>(null)
  const [isRefreshing, setIsRefreshing] = useState(false)

  // Use SignalR stats if available, otherwise use REST API stats
  const stats = signalRStats || restStats

  // Subscribe to all events for the live stream
  useEffect(() => {
    if (connectionState === 'connected') {
      subscribeToAllEvents()
    }
  }, [connectionState, subscribeToAllEvents])

  // Load stats from REST API on mount and when refresh is clicked
  const loadStats = async () => {
    setIsRefreshing(true)
    try {
      const data = await getAuditStats()
      setRestStats(data)
    } catch (err) {
      console.error('Failed to load stats:', err)
    } finally {
      setIsRefreshing(false)
    }
  }

  // Load detailed stats on mount
  useEffect(() => {
    // Load current stats
    loadStats()

    // Load detailed stats for charts
    const loadDetailedStats = async () => {
      try {
        const now = new Date()
        const yesterday = new Date(now.getTime() - 24 * 60 * 60 * 1000)
        const data = await getDetailedAuditStats(yesterday.toISOString(), now.toISOString())
        setDetailedStats(data)
      } catch (err) {
        console.error('Failed to load detailed stats:', err)
      }
    }
    loadDetailedStats()
  }, [])

  // Handle search
  const handleSearch = async () => {
    if (!searchQuery.trim()) {
      setSearchResults(null)
      return
    }

    setIsSearching(true)
    try {
      const results = await searchAuditLogs({ query: searchQuery, pageSize: 20 })
      setSearchResults(results)
    } catch (err) {
      console.error('Search failed:', err)
    } finally {
      setIsSearching(false)
    }
  }

  // Prepare chart data from hourlyActivity (from stats) or dailyActivity (from detailedStats)
  const hourlyChartData = useMemo(() => {
    // Use hourlyActivity from current stats (if available) for last 24h breakdown
    if (stats?.hourlyActivity?.length) {
      return stats.hourlyActivity.map(h => ({
        hour: `${h.hour.toString().padStart(2, '0')}:00`,
        http: h.httpRequests,
        entities: h.entityChanges,
        errors: h.errors,
      }))
    }
    return []
  }, [stats])

  // Entity type breakdown from detailed stats
  const entityPieData = useMemo(() => {
    if (!detailedStats?.entityTypeBreakdown?.length) return []
    return detailedStats.entityTypeBreakdown.slice(0, 5).map(e => ({
      name: e.entityType,
      value: e.total,
    }))
  }, [detailedStats])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Audit Dashboard</h1>
          <p className="text-muted-foreground">Real-time audit event monitoring</p>
        </div>
        <div className="flex items-center gap-4">
          <ConnectionStatus state={connectionState} />
          <Button variant="outline" size="sm" onClick={loadStats} disabled={isRefreshing}>
            <RefreshCw className={`h-4 w-4 mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>
      </div>

      {/* Stats Cards - Today's Activity */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="HTTP Requests"
          value={stats?.todayHttpRequests || 0}
          subtitle="Today"
          icon={Globe}
        />
        <StatCard
          title="Handler Executions"
          value={stats?.todayHandlerExecutions || 0}
          subtitle="Today"
          icon={Settings}
        />
        <StatCard
          title="Entity Changes"
          value={stats?.todayEntityChanges || 0}
          subtitle="Today"
          icon={Database}
        />
        <StatCard
          title="Active Users"
          value={stats?.activeUsers || 0}
          subtitle="Today"
          icon={Activity}
        />
      </div>

      {/* Performance & Errors */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Avg Response Time</CardTitle>
            <Clock className="h-4 w-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{(stats?.avgResponseTimeMs || 0).toFixed(0)}ms</div>
            <p className="text-xs text-muted-foreground">HTTP requests</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Errors Today</CardTitle>
            <Zap className="h-4 w-4 text-red-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-500">{formatNumber(stats?.todayErrors || 0)}</div>
            <p className="text-xs text-muted-foreground">Handler failures</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Events</CardTitle>
            <FileText className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {formatNumber((stats?.todayHttpRequests || 0) + (stats?.todayHandlerExecutions || 0) + (stats?.todayEntityChanges || 0))}
            </div>
            <p className="text-xs text-muted-foreground">All event types today</p>
          </CardContent>
        </Card>
      </div>

      {/* Charts and Stream */}
      <div className="grid gap-4 lg:grid-cols-2">
        {/* Hourly Trend Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Activity (24h)</CardTitle>
            <CardDescription>Hourly breakdown of events</CardDescription>
          </CardHeader>
          <CardContent>
            {hourlyChartData.length > 0 ? (
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={hourlyChartData}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="hour" className="text-xs" tick={{ fontSize: 10 }} />
                  <YAxis className="text-xs" />
                  <Tooltip />
                  <Line type="monotone" dataKey="http" stroke="#3b82f6" name="HTTP Requests" strokeWidth={2} />
                  <Line type="monotone" dataKey="entities" stroke="#10b981" name="Entity Changes" strokeWidth={2} />
                  <Line type="monotone" dataKey="errors" stroke="#ef4444" name="Errors" strokeWidth={2} />
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex items-center justify-center h-[300px] text-muted-foreground">
                No activity data available
              </div>
            )}
          </CardContent>
        </Card>

        {/* Entity Distribution Pie Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Top Entity Types</CardTitle>
            <CardDescription>Distribution of audited entities</CardDescription>
          </CardHeader>
          <CardContent>
            {entityPieData.length > 0 ? (
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={entityPieData}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={({ name, percent }) => `${name} (${(percent * 100).toFixed(0)}%)`}
                    outerRadius={100}
                    fill="#8884d8"
                    dataKey="value"
                  >
                    {entityPieData.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={CHART_COLORS[index % CHART_COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex items-center justify-center h-[300px] text-muted-foreground">
                No entity data available
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Search and Live Stream */}
      <div className="grid gap-4 lg:grid-cols-2">
        {/* Search */}
        <Card>
          <CardHeader>
            <CardTitle>Search Audit Logs</CardTitle>
            <CardDescription>Full-text search across all audit events</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex gap-2 mb-4">
              <Input
                placeholder="Search audit logs..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
              />
              <Button onClick={handleSearch} disabled={isSearching}>
                <Search className="h-4 w-4" />
              </Button>
            </div>
            {searchResults && (
              <div className="space-y-2 max-h-64 overflow-y-auto">
                {searchResults.hits.length === 0 ? (
                  <p className="text-sm text-muted-foreground text-center py-4">No results found</p>
                ) : (
                  searchResults.hits.map((hit) => (
                    <div key={hit.id} className="p-2 rounded border text-sm">
                      <div className="flex items-center gap-2">
                        <Badge variant="outline" className={`text-xs ${getEventTypeColor(hit.type as AuditEventType)}`}>
                          {hit.type}
                        </Badge>
                        <span className="font-medium">{hit.title}</span>
                      </div>
                      {hit.snippet && (
                        <p className="text-xs text-muted-foreground mt-1" dangerouslySetInnerHTML={{ __html: hit.snippet }} />
                      )}
                      <div className="text-xs text-muted-foreground mt-1">
                        {hit.userEmail && <span className="mr-2">{hit.userEmail}</span>}
                        <span>{new Date(hit.timestamp).toLocaleString()}</span>
                      </div>
                    </div>
                  ))
                )}
                {searchResults.totalCount > searchResults.pageSize && (
                  <p className="text-xs text-muted-foreground text-center pt-2">
                    Showing {searchResults.hits.length} of {searchResults.totalCount} results
                  </p>
                )}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Live Event Stream */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              Live Event Stream
              {connectionState === 'connected' && (
                <span className="relative flex h-2 w-2">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-2 w-2 bg-green-500"></span>
                </span>
              )}
            </CardTitle>
            <CardDescription>Real-time audit events</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-1 max-h-80 overflow-y-auto">
              {recentEvents.length === 0 ? (
                <p className="text-sm text-muted-foreground text-center py-8">
                  {connectionState === 'connected' ? 'Waiting for events...' : 'Connect to see live events'}
                </p>
              ) : (
                recentEvents.slice(0, 20).map((event, index) => (
                  <EventStreamItem key={`${event.timestamp}-${index}`} event={event} />
                ))
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

export default AuditDashboard
