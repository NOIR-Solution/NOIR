/**
 * NOIR Theme Configuration
 * =========================
 *
 * This file defines the brand colors and theme for the application.
 * When setting up for a new customer, modify the SELECTED_THEME below.
 *
 * HOW TO CUSTOMIZE:
 * 1. Find a preset that matches the customer's brand (see themes object below)
 * 2. Change SELECTED_THEME to that preset name
 * 3. Or create a new preset by copying an existing one and modifying the colors
 *
 * WHY STATIC PRESETS?
 * Tailwind CSS v4 uses static analysis at build time. Dynamic class generation
 * like `bg-${color}-600` won't work because Tailwind can't detect these classes.
 * Using static presets ensures all classes are included in the final CSS bundle.
 */

/**
 * Theme presets with all Tailwind classes pre-defined
 * Each preset contains the exact CSS classes that Tailwind can detect at build time
 */
const themes = {
  /**
   * Blue/Indigo - Default technology theme
   */
  blue: {
    name: "Blue/Indigo",
    description: "Default technology theme",
    // Gradient backgrounds
    gradient: "bg-gradient-to-br from-blue-600 via-indigo-600 to-blue-700",
    gradientSimple: "bg-gradient-to-br from-blue-600 to-indigo-600",
    // Solid backgrounds
    bgPrimary: "bg-blue-600",
    bgPrimaryHover: "hover:bg-blue-700",
    // Text colors
    textPrimary: "text-blue-600",
    textPrimaryHover: "hover:text-blue-700",
    // Border colors
    borderPrimary: "border-blue-600",
    // Shadow colors
    shadowPrimary: "shadow-blue-500/30",
    shadowPrimaryLight: "shadow-blue-500/20",
    // Ring/focus colors
    ringPrimary: "ring-blue-500/20",
    // Animated blob colors (for login page decorations)
    blobPrimary: "bg-blue-400/20",
    blobSecondary: "bg-indigo-400/20",
    blobAccent: "bg-cyan-400/20",
    // Button combinations
    buttonPrimary: "bg-blue-600 hover:bg-blue-700 text-white shadow-lg shadow-blue-500/20",
    // Link combinations
    linkPrimary: "text-blue-600 hover:text-blue-700",
    // Icon container (gradient box)
    iconContainer: "bg-gradient-to-br from-blue-600 to-indigo-600",
    iconContainerShadow: "shadow-lg shadow-blue-500/30",
    // SVG gradient colors (for GradientWave component)
    svgGradientStart: "#3b82f6", // blue-500
    svgGradientEnd: "#6366f1",   // indigo-500
  },

  /**
   * Slate/Blue - Corporate professional theme
   */
  corporate: {
    name: "Slate/Blue",
    description: "Corporate professional theme",
    gradient: "bg-gradient-to-br from-slate-600 via-blue-600 to-slate-700",
    gradientSimple: "bg-gradient-to-br from-slate-600 to-blue-600",
    bgPrimary: "bg-slate-600",
    bgPrimaryHover: "hover:bg-slate-700",
    textPrimary: "text-slate-600",
    textPrimaryHover: "hover:text-slate-700",
    borderPrimary: "border-slate-600",
    shadowPrimary: "shadow-slate-500/30",
    shadowPrimaryLight: "shadow-slate-500/20",
    ringPrimary: "ring-slate-500/20",
    blobPrimary: "bg-slate-400/20",
    blobSecondary: "bg-blue-400/20",
    blobAccent: "bg-sky-400/20",
    buttonPrimary: "bg-slate-600 hover:bg-slate-700 text-white shadow-lg shadow-slate-500/20",
    linkPrimary: "text-slate-600 hover:text-slate-700",
    iconContainer: "bg-gradient-to-br from-slate-600 to-blue-600",
    iconContainerShadow: "shadow-lg shadow-slate-500/30",
    svgGradientStart: "#64748b",
    svgGradientEnd: "#3b82f6",
  },

  /**
   * Teal/Cyan - Healthcare/medical theme
   */
  healthcare: {
    name: "Teal/Cyan",
    description: "Healthcare/medical theme",
    gradient: "bg-gradient-to-br from-teal-600 via-cyan-600 to-teal-700",
    gradientSimple: "bg-gradient-to-br from-teal-600 to-cyan-600",
    bgPrimary: "bg-teal-600",
    bgPrimaryHover: "hover:bg-teal-700",
    textPrimary: "text-teal-600",
    textPrimaryHover: "hover:text-teal-700",
    borderPrimary: "border-teal-600",
    shadowPrimary: "shadow-teal-500/30",
    shadowPrimaryLight: "shadow-teal-500/20",
    ringPrimary: "ring-teal-500/20",
    blobPrimary: "bg-teal-400/20",
    blobSecondary: "bg-cyan-400/20",
    blobAccent: "bg-emerald-400/20",
    buttonPrimary: "bg-teal-600 hover:bg-teal-700 text-white shadow-lg shadow-teal-500/20",
    linkPrimary: "text-teal-600 hover:text-teal-700",
    iconContainer: "bg-gradient-to-br from-teal-600 to-cyan-600",
    iconContainerShadow: "shadow-lg shadow-teal-500/30",
    svgGradientStart: "#14b8a6",
    svgGradientEnd: "#06b6d4",
  },

  /**
   * Emerald/Green - Finance/banking theme
   */
  finance: {
    name: "Emerald/Green",
    description: "Finance/banking theme",
    gradient: "bg-gradient-to-br from-emerald-600 via-green-600 to-emerald-700",
    gradientSimple: "bg-gradient-to-br from-emerald-600 to-green-600",
    bgPrimary: "bg-emerald-600",
    bgPrimaryHover: "hover:bg-emerald-700",
    textPrimary: "text-emerald-600",
    textPrimaryHover: "hover:text-emerald-700",
    borderPrimary: "border-emerald-600",
    shadowPrimary: "shadow-emerald-500/30",
    shadowPrimaryLight: "shadow-emerald-500/20",
    ringPrimary: "ring-emerald-500/20",
    blobPrimary: "bg-emerald-400/20",
    blobSecondary: "bg-green-400/20",
    blobAccent: "bg-teal-400/20",
    buttonPrimary: "bg-emerald-600 hover:bg-emerald-700 text-white shadow-lg shadow-emerald-500/20",
    linkPrimary: "text-emerald-600 hover:text-emerald-700",
    iconContainer: "bg-gradient-to-br from-emerald-600 to-green-600",
    iconContainerShadow: "shadow-lg shadow-emerald-500/30",
    svgGradientStart: "#10b981",
    svgGradientEnd: "#22c55e",
  },

  /**
   * Purple/Pink - Creative/design theme
   */
  creative: {
    name: "Purple/Pink",
    description: "Creative/design theme",
    gradient: "bg-gradient-to-br from-purple-600 via-pink-600 to-purple-700",
    gradientSimple: "bg-gradient-to-br from-purple-600 to-pink-600",
    bgPrimary: "bg-purple-600",
    bgPrimaryHover: "hover:bg-purple-700",
    textPrimary: "text-purple-600",
    textPrimaryHover: "hover:text-purple-700",
    borderPrimary: "border-purple-600",
    shadowPrimary: "shadow-purple-500/30",
    shadowPrimaryLight: "shadow-purple-500/20",
    ringPrimary: "ring-purple-500/20",
    blobPrimary: "bg-purple-400/20",
    blobSecondary: "bg-pink-400/20",
    blobAccent: "bg-fuchsia-400/20",
    buttonPrimary: "bg-purple-600 hover:bg-purple-700 text-white shadow-lg shadow-purple-500/20",
    linkPrimary: "text-purple-600 hover:text-purple-700",
    iconContainer: "bg-gradient-to-br from-purple-600 to-pink-600",
    iconContainerShadow: "shadow-lg shadow-purple-500/30",
    svgGradientStart: "#a855f7",
    svgGradientEnd: "#ec4899",
  },

  /**
   * Orange/Amber - Energy/industrial theme
   */
  energy: {
    name: "Orange/Amber",
    description: "Energy/industrial theme",
    gradient: "bg-gradient-to-br from-orange-600 via-amber-600 to-orange-700",
    gradientSimple: "bg-gradient-to-br from-orange-600 to-amber-600",
    bgPrimary: "bg-orange-600",
    bgPrimaryHover: "hover:bg-orange-700",
    textPrimary: "text-orange-600",
    textPrimaryHover: "hover:text-orange-700",
    borderPrimary: "border-orange-600",
    shadowPrimary: "shadow-orange-500/30",
    shadowPrimaryLight: "shadow-orange-500/20",
    ringPrimary: "ring-orange-500/20",
    blobPrimary: "bg-orange-400/20",
    blobSecondary: "bg-amber-400/20",
    blobAccent: "bg-yellow-400/20",
    buttonPrimary: "bg-orange-600 hover:bg-orange-700 text-white shadow-lg shadow-orange-500/20",
    linkPrimary: "text-orange-600 hover:text-orange-700",
    iconContainer: "bg-gradient-to-br from-orange-600 to-amber-600",
    iconContainerShadow: "shadow-lg shadow-orange-500/30",
    svgGradientStart: "#ea580c",
    svgGradientEnd: "#d97706",
  },

  /**
   * Rose/Pink - Modern/trendy theme
   */
  modern: {
    name: "Rose/Pink",
    description: "Modern/trendy theme",
    gradient: "bg-gradient-to-br from-rose-600 via-pink-600 to-rose-700",
    gradientSimple: "bg-gradient-to-br from-rose-600 to-pink-600",
    bgPrimary: "bg-rose-600",
    bgPrimaryHover: "hover:bg-rose-700",
    textPrimary: "text-rose-600",
    textPrimaryHover: "hover:text-rose-700",
    borderPrimary: "border-rose-600",
    shadowPrimary: "shadow-rose-500/30",
    shadowPrimaryLight: "shadow-rose-500/20",
    ringPrimary: "ring-rose-500/20",
    blobPrimary: "bg-rose-400/20",
    blobSecondary: "bg-pink-400/20",
    blobAccent: "bg-fuchsia-400/20",
    buttonPrimary: "bg-rose-600 hover:bg-rose-700 text-white shadow-lg shadow-rose-500/20",
    linkPrimary: "text-rose-600 hover:text-rose-700",
    iconContainer: "bg-gradient-to-br from-rose-600 to-pink-600",
    iconContainerShadow: "shadow-lg shadow-rose-500/30",
    svgGradientStart: "#e11d48",
    svgGradientEnd: "#ec4899",
  },

  /**
   * Indigo/Violet - Premium/luxury theme
   */
  premium: {
    name: "Indigo/Violet",
    description: "Premium/luxury theme",
    gradient: "bg-gradient-to-br from-indigo-600 via-violet-600 to-indigo-700",
    gradientSimple: "bg-gradient-to-br from-indigo-600 to-violet-600",
    bgPrimary: "bg-indigo-600",
    bgPrimaryHover: "hover:bg-indigo-700",
    textPrimary: "text-indigo-600",
    textPrimaryHover: "hover:text-indigo-700",
    borderPrimary: "border-indigo-600",
    shadowPrimary: "shadow-indigo-500/30",
    shadowPrimaryLight: "shadow-indigo-500/20",
    ringPrimary: "ring-indigo-500/20",
    blobPrimary: "bg-indigo-400/20",
    blobSecondary: "bg-violet-400/20",
    blobAccent: "bg-purple-400/20",
    buttonPrimary: "bg-indigo-600 hover:bg-indigo-700 text-white shadow-lg shadow-indigo-500/20",
    linkPrimary: "text-indigo-600 hover:text-indigo-700",
    iconContainer: "bg-gradient-to-br from-indigo-600 to-violet-600",
    iconContainerShadow: "shadow-lg shadow-indigo-500/30",
    svgGradientStart: "#4f46e5",
    svgGradientEnd: "#8b5cf6",
  },
} as const

/**
 * SELECTED THEME
 * ==============
 * Change this value to switch the entire application's color scheme.
 * Available options: "blue" | "corporate" | "healthcare" | "finance" | "creative" | "energy" | "modern" | "premium"
 */
export const SELECTED_THEME: keyof typeof themes = "blue"

/**
 * Export the selected theme's classes for use in components
 */
export const themeClasses = themes[SELECTED_THEME]
