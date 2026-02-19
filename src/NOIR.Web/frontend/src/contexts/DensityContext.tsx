import {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  type ReactNode,
} from 'react'

export type Density = 'compact' | 'comfortable' | 'spacious'

interface DensityContextType {
  /** Current density setting */
  density: Density
  /** Set density preference */
  setDensity: (density: Density) => void
}

const DensityContext = createContext<DensityContextType | undefined>(undefined)

const STORAGE_KEY = 'noir-density-preference'
const DEFAULT_DENSITY: Density = 'comfortable'

const isValidDensity = (value: string | null): value is Density =>
  value === 'compact' || value === 'comfortable' || value === 'spacious'

const applyDensity = (density: Density) => {
  document.documentElement.setAttribute('data-density', density)
}

interface DensityProviderProps {
  children: ReactNode
  defaultDensity?: Density
}

export const DensityProvider = ({
  children,
  defaultDensity = DEFAULT_DENSITY,
}: DensityProviderProps) => {
  const [density, setDensityState] = useState<Density>(() => {
    if (typeof window === 'undefined') return defaultDensity
    const stored = localStorage.getItem(STORAGE_KEY)
    return isValidDensity(stored) ? stored : defaultDensity
  })

  // Apply density on mount and when it changes
  useEffect(() => {
    applyDensity(density)
  }, [density])

  const setDensity = useCallback((newDensity: Density) => {
    setDensityState(newDensity)
    localStorage.setItem(STORAGE_KEY, newDensity)
  }, [])

  return (
    <DensityContext.Provider value={{ density, setDensity }}>
      {children}
    </DensityContext.Provider>
  )
}

export const useDensity = () => {
  const context = useContext(DensityContext)
  if (context === undefined) {
    throw new Error('useDensity must be used within a DensityProvider')
  }
  return context
}
