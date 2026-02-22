import { useState, useEffect, useCallback } from 'react'

const ONBOARDING_STORAGE_KEY = 'noir-onboarding'

interface OnboardingState {
  /** Whether the welcome modal has been shown */
  welcomeShown: boolean
  /** Whether the guided tour has been completed */
  tourCompleted: boolean
  /** Completed checklist items */
  completedItems: string[]
  /** Timestamp when onboarding was last updated */
  updatedAt: string
}

const DEFAULT_STATE: OnboardingState = {
  welcomeShown: false,
  tourCompleted: false,
  completedItems: [],
  updatedAt: new Date().toISOString(),
}

/**
 * Onboarding checklist items
 * Uses i18n keys (labelKey/descriptionKey) resolved by the rendering component
 */
export const ONBOARDING_ITEMS = [
  {
    id: 'profile',
    labelKey: 'onboarding.items.profile.label',
    descriptionKey: 'onboarding.items.profile.description',
    href: '/portal/settings',
  },
  {
    id: 'explore',
    labelKey: 'onboarding.items.explore.label',
    descriptionKey: 'onboarding.items.explore.description',
    href: '/portal',
  },
  {
    id: 'product',
    labelKey: 'onboarding.items.product.label',
    descriptionKey: 'onboarding.items.product.description',
    href: '/portal/ecommerce/products/new',
  },
  {
    id: 'blog',
    labelKey: 'onboarding.items.blog.label',
    descriptionKey: 'onboarding.items.blog.description',
    href: '/portal/blog/posts/new',
  },
  {
    id: 'team',
    labelKey: 'onboarding.items.team.label',
    descriptionKey: 'onboarding.items.team.description',
    href: '/portal/admin/users',
  },
] as const

export type OnboardingItemId = (typeof ONBOARDING_ITEMS)[number]['id']

/**
 * Hook for managing onboarding state
 */
export const useOnboarding = () => {
  const [state, setState] = useState<OnboardingState>(() => {
    if (typeof window === 'undefined') return DEFAULT_STATE

    try {
      const stored = localStorage.getItem(ONBOARDING_STORAGE_KEY)
      if (stored) {
        return JSON.parse(stored) as OnboardingState
      }
    } catch {
      // Invalid stored state, use default
    }
    return DEFAULT_STATE
  })

  // Persist state to localStorage
  useEffect(() => {
    try {
      localStorage.setItem(ONBOARDING_STORAGE_KEY, JSON.stringify(state))
    } catch {
      // Storage not available
    }
  }, [state])

  const markWelcomeShown = useCallback(() => {
    setState((prev) => ({
      ...prev,
      welcomeShown: true,
      updatedAt: new Date().toISOString(),
    }))
  }, [])

  const markTourCompleted = useCallback(() => {
    setState((prev) => ({
      ...prev,
      tourCompleted: true,
      updatedAt: new Date().toISOString(),
    }))
  }, [])

  const completeItem = useCallback((itemId: OnboardingItemId) => {
    setState((prev) => {
      if (prev.completedItems.includes(itemId)) return prev
      return {
        ...prev,
        completedItems: [...prev.completedItems, itemId],
        updatedAt: new Date().toISOString(),
      }
    })
  }, [])

  const uncompleteItem = useCallback((itemId: OnboardingItemId) => {
    setState((prev) => ({
      ...prev,
      completedItems: prev.completedItems.filter((id) => id !== itemId),
      updatedAt: new Date().toISOString(),
    }))
  }, [])

  const resetOnboarding = useCallback(() => {
    setState(DEFAULT_STATE)
  }, [])

  const isItemCompleted = useCallback(
    (itemId: OnboardingItemId) => state.completedItems.includes(itemId),
    [state.completedItems]
  )

  const progress = {
    completed: state.completedItems.length,
    total: ONBOARDING_ITEMS.length,
    percentage: Math.round(
      (state.completedItems.length / ONBOARDING_ITEMS.length) * 100
    ),
  }

  const shouldShowWelcome = !state.welcomeShown
  const shouldShowChecklist =
    state.welcomeShown && progress.completed < progress.total

  return {
    state,
    markWelcomeShown,
    markTourCompleted,
    completeItem,
    uncompleteItem,
    resetOnboarding,
    isItemCompleted,
    progress,
    shouldShowWelcome,
    shouldShowChecklist,
    items: ONBOARDING_ITEMS,
  }
}
