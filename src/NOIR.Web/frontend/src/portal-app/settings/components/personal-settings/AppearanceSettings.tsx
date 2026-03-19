import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { useDensity, type Density } from '@/contexts/DensityContext'

const DensityCompactIcon = ({ className }: { className?: string }) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className={className} aria-hidden="true">
    <line x1="4" y1="6" x2="20" y2="6" />
    <line x1="4" y1="10" x2="20" y2="10" />
    <line x1="4" y1="14" x2="20" y2="14" />
    <line x1="4" y1="18" x2="20" y2="18" />
  </svg>
)

const DensityNormalIcon = ({ className }: { className?: string }) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className={className} aria-hidden="true">
    <line x1="4" y1="5" x2="20" y2="5" />
    <line x1="4" y1="12" x2="20" y2="12" />
    <line x1="4" y1="19" x2="20" y2="19" />
  </svg>
)

const DensityComfortableIcon = ({ className }: { className?: string }) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" className={className} aria-hidden="true">
    <line x1="4" y1="7" x2="20" y2="7" />
    <line x1="4" y1="17" x2="20" y2="17" />
  </svg>
)

interface DensityOption {
  value: Density
  labelKey: string
  descriptionKey: string
  icon: typeof DensityCompactIcon
}

const densityOptions: DensityOption[] = [
  {
    value: 'compact',
    labelKey: 'density.compact',
    descriptionKey: 'density.compactDescription',
    icon: DensityCompactIcon,
  },
  {
    value: 'normal',
    labelKey: 'density.normal',
    descriptionKey: 'density.normalDescription',
    icon: DensityNormalIcon,
  },
  {
    value: 'comfortable',
    labelKey: 'density.comfortable',
    descriptionKey: 'density.comfortableDescription',
    icon: DensityComfortableIcon,
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
