/**
 * Developer Logs Page
 *
 * Real-time log viewer with SignalR streaming, syntax highlighting,
 * log level control, filtering, historical log browsing, and error clustering.
 *
 * This is the orchestrator component that manages top-level state and
 * delegates rendering to child components in the `components/` directory.
 */
import { useState, useEffect, useCallback, useMemo, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { usePageContext } from '@/hooks/usePageContext'
import { useLogStream } from '@/hooks/useLogStream'
import {
  Terminal,
  Wifi,
  WifiOff,
  RefreshCw,
  History,
  BarChart3,
  AlertCircle,
} from 'lucide-react'
import { Badge, PageHeader, Tabs, TabsContent, TabsList, TabsTrigger } from '@uikit'

import { isPlatformAdmin } from '@/lib/roles'
import { useAuthContext } from '@/contexts/AuthContext'
import {
  getLogLevel,
  setLogLevel,
  clearBuffer,
  type DevLogLevel,
} from '@/services/developerLogs'
import {
  LOG_STREAM_CONFIG,
  LOG_LEVELS,
  LiveLogsToolbar,
  LogTable,
  LogDetailDialog,
  HistoryTab,
  StatsTab,
  ErrorClustersTab,
} from './components'

export default function DeveloperLogsPage() {
  useTranslation('common')
  usePageContext('Developer Logs')

  const { user } = useAuthContext()

  /**
   * LogStream hub requires SystemAdmin permission (Permissions.SystemAdmin).
   * Only Platform Admins have this permission.
   * Tenant admins should not auto-connect to prevent 403 errors.
   */
  const canAccessLogStream = isPlatformAdmin(user?.roles)

  // Log stream hook
  const {
    connectionState,
    entries,
    bufferStats,
    errorClusters,
    isPaused,
    isConnected,
    setPaused,
    clearEntries,
    requestErrorSummary,
    requestBufferStats,
  } = useLogStream({
    autoConnect: canAccessLogStream && LOG_STREAM_CONFIG.AUTO_CONNECT,
    maxEntries: LOG_STREAM_CONFIG.MAX_ENTRIES,
  })

  // Local state
  const [serverLevel, setServerLevel] = useState<string>('Information')
  const [availableLevels, setAvailableLevels] = useState<string[]>([])
  const [searchTerm, setSearchTerm] = useState('')
  const [exceptionsOnly, setExceptionsOnly] = useState(false)
  const [liveSelectedLevels, setLiveSelectedLevels] = useState<Set<DevLogLevel>>(new Set())
  const [expandedEntries, setExpandedEntries] = useState<Set<number>>(new Set())
  const [isChangingLevel, setIsChangingLevel] = useState(false)
  const [autoScroll, setAutoScroll] = useState(true)
  const [sortOrder, setSortOrder] = useState<'newest' | 'oldest'>('newest')
  const [mainTab, setMainTab] = useState('live')
  const [detailEntry, setDetailEntry] = useState<import('@/services/developerLogs').LogEntryDto | null>(null)
  const [isLiveFullscreen, setIsLiveFullscreen] = useState(false)

  const scrollAreaRef = useRef<HTMLDivElement>(null)
  const lastEntryCountRef = useRef(entries.length)

  // Auto-scroll when new entries arrive
  useEffect(() => {
    if (autoScroll && entries.length > lastEntryCountRef.current && scrollAreaRef.current) {
      const viewport = scrollAreaRef.current.querySelector('[data-radix-scroll-area-viewport]')
      if (viewport) {
        viewport.scrollTop = 0
      }
    }
    lastEntryCountRef.current = entries.length
  }, [entries.length, autoScroll])

  // Fetch initial log level
  useEffect(() => {
    getLogLevel().then(response => {
      setServerLevel(response.level)
      setAvailableLevels(response.availableLevels)
    }).catch(() => { /* Error visible in network tab */ })
  }, [])

  // Handle log level change
  const handleLevelChange = async (level: string) => {
    setIsChangingLevel(true)
    try {
      const response = await setLogLevel(level)
      setServerLevel(response.level)

      // Sync display filter to match server level
      const levelIndex = LOG_LEVELS.findIndex(l => l.value === level)
      if (levelIndex >= 0) {
        const levelsToShow = new Set<DevLogLevel>(
          LOG_LEVELS.slice(levelIndex).map(l => l.value)
        )
        setLiveSelectedLevels(levelsToShow)
      }
    } catch {
      // Error visible in network tab
    } finally {
      setIsChangingLevel(false)
    }
  }

  // Handle clear buffer
  const handleClearBuffer = async () => {
    try {
      await clearBuffer()
      clearEntries()
      refreshStats()
    } catch {
      // Error visible in network tab
    }
  }

  // Refresh stats via SignalR
  const refreshStats = useCallback(() => {
    requestBufferStats()
    requestErrorSummary()
  }, [requestBufferStats, requestErrorSummary])

  // Toggle entry expansion
  const toggleEntryExpanded = useCallback((id: number) => {
    setExpandedEntries(prev => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }, [])

  // Filter entries locally
  const filteredEntries = useMemo(() => {
    let result = entries.filter(entry => {
      // Level filter
      if (liveSelectedLevels.size > 0) {
        const entryLevel = String(entry.level)
        const isLevelSelected = Array.from(liveSelectedLevels).some(
          selectedLevel => selectedLevel.toLowerCase() === entryLevel.toLowerCase()
        )
        if (!isLevelSelected) return false
      }

      // Search filter
      if (searchTerm) {
        const searchLower = searchTerm.toLowerCase()
        const matchesMessage = entry.message.toLowerCase().includes(searchLower)
        const matchesSource = entry.sourceContext?.toLowerCase().includes(searchLower)
        const matchesException = entry.exception?.message?.toLowerCase().includes(searchLower)
        if (!matchesMessage && !matchesSource && !matchesException) return false
      }

      // Errors only filter
      if (exceptionsOnly) {
        const levelLower = String(entry.level).toLowerCase()
        const isError = levelLower === 'error' || levelLower === 'warning' || levelLower === 'fatal' || entry.exception
        if (!isError) return false
      }

      return true
    })

    // Apply sort order
    if (sortOrder === 'oldest') {
      result = [...result].reverse()
    }

    return result
  }, [entries, searchTerm, exceptionsOnly, sortOrder, liveSelectedLevels])

  return (
    <div className="flex flex-col h-[calc(100vh-48px)] overflow-hidden">
      <PageHeader
        icon={Terminal}
        title="Developer Logs"
        description="Real-time server log streaming and analysis"
        action={
          <div className="flex items-center gap-2">
          {isConnected ? (
            <Badge variant="outline" className="gap-1 bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-400">
              <Wifi className="h-3 w-3" />
              Connected
            </Badge>
          ) : connectionState === 'connecting' || connectionState === 'reconnecting' ? (
            <Badge variant="outline" className="gap-1 bg-amber-50 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
              <RefreshCw className="h-3 w-3 animate-spin" />
              {connectionState === 'connecting' ? 'Connecting' : 'Reconnecting'}
            </Badge>
          ) : (
            <Badge variant="outline" className="gap-1 bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-400">
              <WifiOff className="h-3 w-3" />
              Disconnected
            </Badge>
          )}
          </div>
        }
      />

      {/* Main tabs */}
      <Tabs value={mainTab} onValueChange={setMainTab} className="flex-1 flex flex-col mt-4 overflow-hidden">
        <TabsList>
          <TabsTrigger value="live" className="gap-2">
            <Terminal className="h-4 w-4" />
            Live Logs
          </TabsTrigger>
          <TabsTrigger value="history" className="gap-2">
            <History className="h-4 w-4" />
            History Files
          </TabsTrigger>
          <TabsTrigger value="stats" className="gap-2">
            <BarChart3 className="h-4 w-4" />
            Statistics
          </TabsTrigger>
          <TabsTrigger value="errors" className="gap-2">
            <AlertCircle className="h-4 w-4" />
            Error Clusters
          </TabsTrigger>
        </TabsList>

        {/* Live Logs Tab */}
        <TabsContent value="live" className="space-y-4">
          <LiveLogsToolbar
            isPaused={isPaused}
            onTogglePause={() => setPaused(!isPaused)}
            autoScroll={autoScroll}
            onToggleAutoScroll={() => setAutoScroll(!autoScroll)}
            sortOrder={sortOrder}
            onToggleSortOrder={() => setSortOrder(sortOrder === 'newest' ? 'oldest' : 'newest')}
            serverLevel={serverLevel}
            availableLevels={availableLevels}
            isChangingLevel={isChangingLevel}
            onLevelChange={handleLevelChange}
            selectedLevels={liveSelectedLevels}
            onSelectedLevelsChange={setLiveSelectedLevels}
            searchTerm={searchTerm}
            onSearchTermChange={setSearchTerm}
            exceptionsOnly={exceptionsOnly}
            onExceptionsOnlyChange={setExceptionsOnly}
            onClearBuffer={handleClearBuffer}
          />

          <LogTable
            ref={scrollAreaRef}
            entries={filteredEntries}
            expandedEntries={expandedEntries}
            onToggleExpand={toggleEntryExpanded}
            onViewDetail={setDetailEntry}
            totalEntries={entries.length}
            searchTerm={searchTerm}
            autoScroll={autoScroll}
            isPaused={isPaused}
            emptyMessage="No log entries"
            emptySubMessage={
              entries.length === 0
                ? 'Waiting for incoming logs...'
                : 'No entries match the current filters'
            }
            useScrollArea={true}
            scrollAreaClassName="h-[calc(100vh-330px)] min-h-[400px]"
            isFullscreen={isLiveFullscreen}
            onFullscreenChange={setIsLiveFullscreen}
            fullscreenTitle="Live Logs"
          />

          <LogDetailDialog
            entry={detailEntry}
            open={!!detailEntry}
            onOpenChange={(open) => !open && setDetailEntry(null)}
          />
        </TabsContent>

        {/* History Files Tab */}
        <TabsContent value="history" className="flex-1 mt-4 overflow-hidden">
          <HistoryTab />
        </TabsContent>

        {/* Statistics Tab */}
        <TabsContent value="stats">
          <StatsTab stats={bufferStats} onRefresh={refreshStats} />
        </TabsContent>

        {/* Error Clusters Tab */}
        <TabsContent value="errors">
          <ErrorClustersTab
            clusters={errorClusters}
            onRefresh={() => requestErrorSummary()}
          />
        </TabsContent>
      </Tabs>
    </div>
  )
}
