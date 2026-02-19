import { useTranslation } from 'react-i18next'
import { AlignJustify, AlignCenter, AlignLeft } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useDensity, type Density } from '@/contexts/DensityContext'

interface DensityOption {
  value: Density
  labelKey: string
  descriptionKey: string
  icon: typeof AlignJustify
}

const densityOptions: DensityOption[] = [
  {
    value: 'compact',
    labelKey: 'density.compact',
    descriptionKey: 'density.compactDescription',
    icon: AlignJustify,
  },
  {
    value: 'comfortable',
    labelKey: 'density.comfortable',
    descriptionKey: 'density.comfortableDescription',
    icon: AlignCenter,
  },
  {
    value: 'spacious',
    labelKey: 'density.spacious',
    descriptionKey: 'density.spaciousDescription',
    icon: AlignLeft,
  },
]

export const AppearanceSettings = () => {
  const { t } = useTranslation('common')
  const { density, setDensity } = useDensity()

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold tracking-tight">
          {t('density.label')}
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          {t('density.description')}
        </p>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        {densityOptions.map((option) => {
          const Icon = option.icon
          const isSelected = density === option.value
          return (
            <button
              type="button"
              key={option.value}
              aria-pressed={isSelected}
              onClick={() => setDensity(option.value)}
              className={cn(
                'flex flex-col items-center gap-3 rounded-lg border-2 p-4 transition-all cursor-pointer',
                'hover:border-primary/50 hover:bg-accent/50',
                isSelected
                  ? 'border-primary bg-primary/5 shadow-sm'
                  : 'border-border'
              )}
            >
              <Icon
                className={cn(
                  'h-6 w-6',
                  isSelected ? 'text-primary' : 'text-muted-foreground'
                )}
              />
              <div className="text-center">
                <p className={cn(
                  'text-sm font-medium',
                  isSelected ? 'text-primary' : 'text-foreground'
                )}>
                  {t(option.labelKey)}
                </p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {t(option.descriptionKey)}
                </p>
              </div>
            </button>
          )
        })}
      </div>
    </div>
  )
}
