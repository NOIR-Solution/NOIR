/**
 * Date Range Picker with preset options for reports.
 * Wraps the uikit DateRangePicker with quick-select presets.
 */
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { subDays, startOfDay, endOfDay, startOfMonth, subMonths } from 'date-fns'
import type { DateRange } from 'react-day-picker'
import { DateRangePicker, Button } from '@uikit'

export interface DateRangePresetsProps {
  value?: DateRange
  onChange: (range: DateRange | undefined) => void
}

type PresetKey = 'today' | 'last7' | 'last30' | 'thisMonth' | 'lastMonth' | 'custom'

export const DateRangePresets = ({ value, onChange }: DateRangePresetsProps) => {
  const { t } = useTranslation('common')
  const [activePreset, setActivePreset] = useState<PresetKey>('last30')

  const presets: { key: PresetKey; label: string; getRange: () => DateRange }[] = [
    {
      key: 'today',
      label: t('reports.presets.today', 'Today'),
      getRange: () => ({
        from: startOfDay(new Date()),
        to: endOfDay(new Date()),
      }),
    },
    {
      key: 'last7',
      label: t('reports.presets.last7Days', 'Last 7 days'),
      getRange: () => ({
        from: startOfDay(subDays(new Date(), 7)),
        to: endOfDay(new Date()),
      }),
    },
    {
      key: 'last30',
      label: t('reports.presets.last30Days', 'Last 30 days'),
      getRange: () => ({
        from: startOfDay(subDays(new Date(), 30)),
        to: endOfDay(new Date()),
      }),
    },
    {
      key: 'thisMonth',
      label: t('reports.presets.thisMonth', 'This month'),
      getRange: () => ({
        from: startOfMonth(new Date()),
        to: endOfDay(new Date()),
      }),
    },
    {
      key: 'lastMonth',
      label: t('reports.presets.lastMonth', 'Last month'),
      getRange: () => {
        const lastMonth = subMonths(new Date(), 1)
        return {
          from: startOfMonth(lastMonth),
          to: endOfDay(subDays(startOfMonth(new Date()), 1)),
        }
      },
    },
  ]

  const handlePresetClick = (preset: typeof presets[number]) => {
    setActivePreset(preset.key)
    onChange(preset.getRange())
  }

  const handleCustomChange = (range: DateRange | undefined) => {
    setActivePreset('custom')
    onChange(range)
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      {presets.map((preset) => (
        <Button
          key={preset.key}
          variant={activePreset === preset.key ? 'default' : 'outline'}
          size="sm"
          className="cursor-pointer"
          onClick={() => handlePresetClick(preset)}
        >
          {preset.label}
        </Button>
      ))}
      <DateRangePicker
        value={value}
        onChange={handleCustomChange}
        placeholder={t('reports.presets.custom', 'Custom range')}
        className="h-9 cursor-pointer"
      />
    </div>
  )
}
