import {
  createContext,
  useContext,
  useState,
  useCallback,
  type ReactNode,
} from 'react'

interface CommandContextType {
  /** Whether the command palette is open */
  isOpen: boolean
  /** Open the command palette */
  open: () => void
  /** Close the command palette */
  close: () => void
  /** Toggle the command palette */
  toggle: () => void
}

const CommandContext = createContext<CommandContextType | undefined>(undefined)

interface CommandProviderProps {
  children: ReactNode
}

/**
 * CommandProvider - State management for the command palette
 *
 * Provides open/close/toggle functions to control the command palette visibility.
 */
export const CommandProvider = ({ children }: CommandProviderProps) => {
  const [isOpen, setIsOpen] = useState(false)

  const open = useCallback(() => setIsOpen(true), [])
  const close = useCallback(() => setIsOpen(false), [])
  const toggle = useCallback(() => setIsOpen((prev) => !prev), [])

  return (
    <CommandContext.Provider value={{ isOpen, open, close, toggle }}>
      {children}
    </CommandContext.Provider>
  )
}

/**
 * Hook to access command palette state and controls
 */
export const useCommand = () => {
  const context = useContext(CommandContext)
  if (!context) {
    throw new Error('useCommand must be used within a CommandProvider')
  }
  return context
}
