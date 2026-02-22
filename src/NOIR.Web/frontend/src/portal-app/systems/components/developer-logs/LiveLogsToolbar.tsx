/**
 * LiveLogsToolbar Component
 *
 * Unified toolbar for the live logs tab. Contains playback controls,
 * log level selector, display level filter, search, errors-only toggle,
 * and clear buffer button.
 */
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
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
import {
  Badge,
  Button,
  Card,
  CardContent,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Switch,
} from '@uikit'

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

export const LiveLogsToolbar = ({
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
}: LiveLogsToolbarProps) => {
  const { t } = useTranslation('common')
  const [showClearConfirm, setShowClearConfirm] = useState(false)
  const hasActiveFilters = searchTerm || exceptionsOnly || selectedLevels.size > 0

  const handleClearConfirm = () => {
    onClearBuffer()
    setShowClearConfirm(false)
  }

  return (
    <>
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
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
                  {t('developerLogs.resume')}
                </>
              ) : (
                <>
                  <Pause className="h-4 w-4" />
                  {t('developerLogs.pause')}
                </>
              )}
            </Button>
            <Button
              variant={autoScroll ? 'secondary' : 'ghost'}
              size="sm"
              onClick={onToggleAutoScroll}
              className="gap-1.5"
              title={t('developerLogs.autoScroll')}
            >
              <ArrowDownToLine className={cn('h-4 w-4', autoScroll && 'text-primary')} />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={onToggleSortOrder}
              title={sortOrder === 'newest' ? t('developerLogs.showingNewestFirst') : t('developerLogs.showingOldestFirst')}
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
            <SelectTrigger className="cursor-pointer w-[160px] h-8" title={t('developerLogs.serverMinLevelTooltip', 'Server minimum log level - also filters display')} aria-label={t('developerLogs.serverMinLevel', 'Server minimum log level')}>
              <span className="text-muted-foreground mr-1">{t('developerLogs.minLabel')}</span>
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
              <Button variant="outline" size="sm" className="h-8 gap-2 cursor-pointer" aria-label={t('developerLogs.filterDisplayedLevels', 'Filter displayed log levels')}>
                <span className="text-muted-foreground">{t('developerLogs.filterColon')}</span>
                {selectedLevels.size === 0 ? (
                  <span>{t('developerLogs.allLevels')}</span>
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
                    {t('developerLogs.clearFilters')}
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
                placeholder={t('developerLogs.searchLogsPlaceholder')}
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
              {t('developerLogs.errorsOnly')}
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
              {t('developerLogs.clear')}
            </Button>
          )}

          {/* Clear buffer */}
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowClearConfirm(true)}
            className="h-9 gap-1.5 text-muted-foreground hover:text-destructive cursor-pointer"
          >
            <Trash2 className="h-4 w-4" />
            {t('developerLogs.clearBuffer')}
          </Button>
        </div>
      </CardContent>
    </Card>

    {/* Clear Buffer Confirmation Dialog */}
    <Credenza open={showClearConfirm} onOpenChange={setShowClearConfirm}>
      <CredenzaContent className="border-destructive/30">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <Trash2 className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <CredenzaTitle>{t('developerLogs.clearBufferTitle', 'Clear Log Buffer')}</CredenzaTitle>
              <CredenzaDescription>
                {t('developerLogs.clearBufferConfirmation', 'Are you sure you want to clear all buffered log entries? This action cannot be undone.')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>
        <CredenzaBody />
        <CredenzaFooter>
          <Button
            variant="outline"
            onClick={() => setShowClearConfirm(false)}
            className="cursor-pointer"
          >
            {t('labels.cancel', 'Cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleClearConfirm}
            className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
          >
            {t('developerLogs.clearBuffer', 'Clear Buffer')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
    </>
  )
}
