import { Suspense } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { useMediaQuery } from '@/hooks/useMediaQuery'
import { PageLoader } from '@uikit'
import { supportsViewTransitions } from '@/hooks/useViewTransition'
import { cn } from '@/lib/utils'

/**
 * Page transition variants (framer-motion fallback)
 * Used only when the browser doesn't support the View Transitions API.
 */
const pageVariants = {
  initial: {
    opacity: 0,
    y: 8,
  },
  enter: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.2,
      ease: [0.25, 0.1, 0.25, 1],
    },
  },
  exit: {
    opacity: 0,
    y: -8,
    transition: {
      duration: 0.15,
      ease: [0.25, 0.1, 0.25, 1],
    },
  },
}

/**
 * Reduced motion variants (framer-motion fallback)
 */
const reducedMotionVariants = {
  initial: {
    opacity: 0,
  },
  enter: {
    opacity: 1,
    transition: {
      duration: 0.1,
    },
  },
  exit: {
    opacity: 0,
    transition: {
      duration: 0.1,
    },
  },
}

interface AnimatedOutletProps {
  /** Custom className for the motion container */
  className?: string
  /** Loading fallback component */
  fallback?: React.ReactNode
}

/**
 * AnimatedOutlet - Page transitions using the View Transitions API
 *
 * When the browser supports the View Transitions API (Chrome 111+, Edge 111+,
 * Firefox 144+, Safari 18+), the outlet uses native compositor-thread animations
 * via CSS view-transition-name. The actual transition is triggered by
 * ViewTransitionLink / useViewTransitionNavigate at navigation time.
 *
 * Falls back to framer-motion AnimatePresence for unsupported browsers.
 */
export function AnimatedOutlet({
  className,
  fallback,
}: AnimatedOutletProps) {
  const location = useLocation()
  const prefersReducedMotion = useMediaQuery('(prefers-reduced-motion: reduce)')

  // Native View Transitions: browser handles animation via CSS pseudo-elements.
  // The transition is started by ViewTransitionLink/useViewTransitionNavigate,
  // which wraps navigation in document.startViewTransition().
  if (supportsViewTransitions) {
    return (
      <div className={cn('vt-main-content', className)}>
        <Suspense fallback={fallback || <PageLoader text="Loading..." />}>
          <Outlet />
        </Suspense>
      </div>
    )
  }

  // Fallback: framer-motion transitions for unsupported browsers
  const variants = prefersReducedMotion ? reducedMotionVariants : pageVariants

  return (
    <AnimatePresence mode="wait" initial={false}>
      <motion.div
        key={location.pathname}
        variants={variants}
        initial="initial"
        animate="enter"
        exit="exit"
        className={className}
      >
        <Suspense fallback={fallback || <PageLoader text="Loading..." />}>
          <Outlet />
        </Suspense>
      </motion.div>
    </AnimatePresence>
  )
}
