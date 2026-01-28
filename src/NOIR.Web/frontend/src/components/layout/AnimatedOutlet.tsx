import { Suspense } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { useMediaQuery } from '@/hooks/useMediaQuery'
import { PageLoader } from '@/components/ui/page-loader'

/**
 * Page transition variants
 * Subtle fade + slide for smooth navigation feel
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
      ease: [0.25, 0.1, 0.25, 1], // ease-out-cubic
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
 * Reduced motion variants (for users who prefer reduced motion)
 * Uses opacity only, no movement
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
 * AnimatedOutlet - Wrapper for react-router Outlet with page transitions
 *
 * Features:
 * - Smooth fade + slide transitions between pages
 * - Respects prefers-reduced-motion
 * - Suspense boundary for lazy-loaded pages
 * - AnimatePresence for exit animations
 *
 * @example
 * // In layout:
 * <main>
 *   <AnimatedOutlet />
 * </main>
 */
export function AnimatedOutlet({
  className,
  fallback,
}: AnimatedOutletProps) {
  const location = useLocation()
  const prefersReducedMotion = useMediaQuery('(prefers-reduced-motion: reduce)')

  // Skip animation entirely if user prefers reduced motion
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
