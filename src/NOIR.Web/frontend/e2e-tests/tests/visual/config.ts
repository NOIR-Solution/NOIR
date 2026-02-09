/**
 * Visual Regression Test Configuration
 *
 * Centralized configuration for visual regression test thresholds.
 * These values determine how many pixels can differ before a test fails.
 */

export const VisualDiffThresholds = {
  /**
   * Full page screenshots - allows for minor font rendering differences across OS.
   * Use for: Complete page captures with lots of content.
   */
  FULL_PAGE: 100,

  /**
   * Component screenshots - tighter tolerance for isolated components.
   * Use for: Individual cards, sections, or grouped elements.
   */
  COMPONENT: 30,

  /**
   * Small UI elements - strictest tolerance.
   * Use for: Buttons, badges, icons, form controls.
   */
  ELEMENT: 20,

  /**
   * Complex layouts with dynamic content.
   * Use for: Pages with charts, graphs, or frequently changing data.
   */
  DYNAMIC_CONTENT: 150,

  /**
   * Form inputs - very strict for accessibility validation.
   * Use for: Input fields, textareas, selects.
   */
  FORM_CONTROL: 20,
} as const

/**
 * Standard viewport sizes for responsive testing.
 */
export const Viewports = {
  /** Mobile - iPhone SE */
  MOBILE: { width: 375, height: 667 },

  /** Tablet - iPad */
  TABLET: { width: 768, height: 1024 },

  /** Desktop - Standard HD */
  DESKTOP: { width: 1280, height: 720 },

  /** Desktop - Full HD */
  DESKTOP_FULL_HD: { width: 1920, height: 1080 },
} as const

/**
 * Common screenshot options for consistent testing.
 */
export const ScreenshotOptions = {
  /** Full page capture with animations disabled */
  fullPage: (threshold: number = VisualDiffThresholds.FULL_PAGE) => ({
    fullPage: true,
    animations: 'disabled' as const,
    maxDiffPixels: threshold,
  }),

  /** Component capture with animations disabled */
  component: (threshold: number = VisualDiffThresholds.COMPONENT) => ({
    animations: 'disabled' as const,
    maxDiffPixels: threshold,
  }),

  /** Element capture with strict tolerance */
  element: (threshold: number = VisualDiffThresholds.ELEMENT) => ({
    animations: 'disabled' as const,
    maxDiffPixels: threshold,
  }),
} as const
