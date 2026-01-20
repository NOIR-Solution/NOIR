import type { Variants } from 'framer-motion'

/**
 * Shared animation variants for consistent animations across the application
 * Uses framer-motion for smooth, performant animations
 */

/**
 * Staggered container animation for lists and grids
 * Children animate sequentially with delay
 *
 * @example
 * ```tsx
 * <motion.div variants={staggerContainerVariants} initial="hidden" animate="visible">
 *   <motion.div variants={fadeSlideUpVariants}>Item 1</motion.div>
 *   <motion.div variants={fadeSlideUpVariants}>Item 2</motion.div>
 * </motion.div>
 * ```
 */
export const staggerContainerVariants: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1,
      delayChildren: 0.2,
    },
  },
}

/**
 * Fade + slide up animation for list items
 * Smooth spring animation for natural feel
 * Use with staggerContainerVariants for sequential animation
 */
export const fadeSlideUpVariants: Variants = {
  hidden: { y: 20, opacity: 0 },
  visible: {
    y: 0,
    opacity: 1,
    transition: {
      type: 'spring' as const,
      stiffness: 300,
      damping: 24,
    },
  },
}

/**
 * Scale animation for interactive cards
 * Hover and selected states with spring physics
 *
 * @example
 * ```tsx
 * <motion.div
 *   variants={cardScaleVariants}
 *   whileHover="hover"
 *   animate={isSelected ? 'selected' : 'idle'}
 * >
 *   Card content
 * </motion.div>
 * ```
 */
export const cardScaleVariants: Variants = {
  idle: { scale: 1 },
  hover: {
    scale: 1.02,
    transition: {
      type: 'spring' as const,
      stiffness: 400,
      damping: 10,
    },
  },
  selected: {
    scale: 1.02,
  },
}

/**
 * Page transition animations for step-based flows
 * Horizontal slide with fade for natural navigation feel
 *
 * @example
 * ```tsx
 * <AnimatePresence mode="wait">
 *   <motion.div
 *     key={currentStep}
 *     initial="enter"
 *     animate="center"
 *     exit="exit"
 *     variants={pageSlideVariants}
 *   >
 *     Step content
 *   </motion.div>
 * </AnimatePresence>
 * ```
 */
export const pageSlideVariants: Variants = {
  enterFromRight: { x: 100, opacity: 0 },
  enterFromLeft: { x: -100, opacity: 0 },
  center: { x: 0, opacity: 1 },
  exitToLeft: { x: -100, opacity: 0 },
  exitToRight: { x: 100, opacity: 0 },
}

/**
 * Fade in animation for simple entrance
 * Use for loading states and simple transitions
 */
export const fadeInVariants: Variants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      duration: 0.3,
    },
  },
}

/**
 * Scale in animation for modals and dialogs
 * Bouncy spring effect for attention
 */
export const scaleInVariants: Variants = {
  hidden: { scale: 0, opacity: 0 },
  visible: {
    scale: 1,
    opacity: 1,
    transition: {
      type: 'spring' as const,
      stiffness: 500,
      damping: 30,
    },
  },
}
