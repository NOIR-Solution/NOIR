/**
 * LiveLogsToolbar Component
 *
 * Unified toolbar for the live logs tab. Contains playback controls,
 * log level selector, display level filter, search, errors-only toggle,
 * and clear buffer button.
 */
import {
  Play,
  Pause,
  Trash2,
  Search,
  ChevronDown,
  X,
  ArrowDown,
  ArrowUp,
  ArrowDownToLine,
} from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { cn } from '@/lib/utils'
import type { DevLogLevel } from '@/services/developerLogs'
import { LOG_LEVELS, getLevelConfig } from './log-utils'

export interface LiveLogsToolbarProps {
  // Playback state
  isPaused: boolean
  onTogglePause: () => void
  autoScroll: boolean
  onToggleAutoScroll: () => void
  sortOrder: 'newest' | 'oldest'
  onToggleSortOrder: () => void
  // Server log level
  serverLevel: string
  availableLevels: string[]
  isChangingLevel: boolean
  onLevelChange: (level: string) => void
  // Display level filter
  selectedLevels: Set<DevLogLevel>
  onSelectedLevelsChange: (levels: Set<DevLogLevel>) => void
  // Search
  searchTerm: string
  onSearchTermChange: (term: string) => void
  // Errors only
  exceptionsOnly: boolean
  onExceptionsOnlyChange: (value: boolean) => void
  // Clear
  onClearBuffer: () => void
}

export function LiveLogsToolbar({
  isPaused,
  onTogglePause,
  autoScroll,
  onToggleAutoScroll,
  sortOrder,
  onToggleSortOrder,
  serverLevel,
  availableLevels,
  isChangingLevel,
  onLevelChange,
  selectedLevels,
  onSelectedLevelsChange,
  searchTerm,
  onSearchTermChange,
  exceptionsOnly,
  onExceptionsOnlyChange,
  onClearBuffer,
}: LiveLogsToolbarProps) {
  const hasActiveFilters = searchTerm || exceptionsOnly || selectedLevels.size > 0

  return (
    <Card>
      <CardContent className="p-4 space-y-3">
        {/* Row 1: Main controls */}
        <div className="flex items-center gap-2">
          {/* Playback Group */}
          <div className="flex items-center gap-1 pr-3 border-r">
            <Button
              variant={isPaused ? 'default' : 'secondary'}
              size="sm"
              onClick={onTogglePause}
              className="gap-1.5"
            >
              {isPaused ? (
                <>
                  <Play className="h-4 w-4" />
                  Resume
                </>
              ) : (
                <>
                  <Pause className="h-4 w-4" />
                  Pause
                </>
              )}
            </Button>
            <Button
              variant={autoScroll ? 'secondary' : 'ghost'}
              size="sm"
              onClick={onToggleAutoScroll}
              className="gap-1.5"
              title="Auto-scroll to new entries"
            >
              <ArrowDownToLine className={cn('h-4 w-4', autoScroll && 'text-primary')} />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={onToggleSortOrder}
              title={sortOrder === 'newest' ? 'Showing newest first' : 'Showing oldest first'}
            >
              {sortOrder === 'newest' ? (
                <ArrowDown className="h-4 w-4" />
              ) : (
                <ArrowUp className="h-4 w-4" />
              )}
            </Button>
          </div>

          {/* Server Log Level - controls what logs are generated */}
          <Select
            value={serverLevel}
            onValueChange={onLevelChange}
            disabled={isChangingLevel}
          >
            <SelectTrigger className="w-[160px] h-8" title="Server minimum log level - also filters display">
              <span className="text-muted-foreground mr-1">Min:</span>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {availableLevels.map(level => {
                const config = getLevelConfig(level as DevLogLevel)
                return (
                  <SelectItem key={level} value={level}>
                    <span className={cn('flex items-center gap-2', config.textColor)}>
                      <config.icon className="h-4 w-4" />
                      {level}
                    </span>
                  </SelectItem>
                )
              })}
            </SelectContent>
          </Select>

          {/* Display Level Filter */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm" className="h-8 gap-2">
                <span className="text-muted-foreground">Filter:</span>
                {selectedLevels.size === 0 ? (
                  <span>All</span>
                ) : (
                  <span className="flex items-center gap-1">
                    {Array.from(selectedLevels).slice(0, 2).map(level => {
                      const config = getLevelConfig(level)
                      return (
                        <Badge
                          key={level}
                          variant="outline"
                          className={cn('px-1.5 py-0 text-xs', config.textColor)}
                        >
                          {config.label}
                        </Badge>
                      )
                    })}
                    {selectedLevels.size > 2 && (
                      <span className="text-xs text-muted-foreground">+{selectedLevels.size - 2}</span>
                    )}
                  </span>
                )}
                <ChevronDown className="h-3.5 w-3.5 opacity-50" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {LOG_LEVELS.map(level => (
                <DropdownMenuCheckboxItem
                  key={level.value}
                  checked={selectedLevels.has(level.value)}
                  onSelect={(e) => e.preventDefault()}
                  onCheckedChange={(checked) => {
                    const next = new Set(selectedLevels)
                    if (checked) {
                      next.add(level.value)
                    } else {
                      next.delete(level.value)
                    }
                    onSelectedLevelsChange(next)
                  }}
                >
                  <level.icon className={cn('h-4 w-4 mr-2', level.textColor)} />
                  <span className={level.textColor}>{level.value}</span>
                </DropdownMenuCheckboxItem>
              ))}
              {selectedLevels.size > 0 && (
                <>
                  <DropdownMenuSeparator />
                  <Button
                    variant="ghost"
                    size="sm"
                    className="w-full h-7 text-xs"
                    onClick={(e) => {
                      e.preventDefault()
                      onSelectedLevelsChange(new Set())
                    }}
                  >
                    Clear filters
                  </Button>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Search - grows to fill space */}
          <div className="flex-1 min-w-[180px] max-w-md">
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search logs..."
                value={searchTerm}
                onChange={(e) => onSearchTermChange(e.target.value)}
                className="pl-8 h-8"
              />
              {searchTerm && (
                <button
                  onClick={() => onSearchTermChange('')}
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>
          </div>

          {/* Errors only toggle */}
          <div className="flex items-center gap-2 px-3 py-1.5 bg-muted rounded-md">
            <Switch
              id="errors-only"
              checked={exceptionsOnly}
              onCheckedChange={onExceptionsOnlyChange}
              className={cn(exceptionsOnly && 'data-[state=checked]:bg-destructive')}
            />
            <Label htmlFor="errors-only" className="text-sm cursor-pointer whitespace-nowrap">
              Errors only
            </Label>
          </div>

          {/* Clear filters - only show when filters are active */}
          {hasActiveFilters && (
            <Button
              variant="ghost"
              size="sm"
              className="h-9 gap-1.5"
              onClick={() => {
                onSearchTermChange('')
                onExceptionsOnlyChange(false)
                onSelectedLevelsChange(new Set())
              }}
            >
              <X className="h-3.5 w-3.5" />
              Clear
            </Button>
          )}

          {/* Clear buffer */}
          <Button
            variant="ghost"
            size="sm"
            onClick={onClearBuffer}
            className="h-9 gap-1.5 text-muted-foreground hover:text-destructive"
          >
            <Trash2 className="h-4 w-4" />
            Clear Buffer
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
