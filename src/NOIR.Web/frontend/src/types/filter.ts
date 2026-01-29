/**
 * Filter-related TypeScript types for Storefront filtering
 * Used by FilterSidebar and facet components
 */

// ============================================================================
// Filter Option Types
// ============================================================================

/**
 * A single filter option with value, label, and optional metadata
 */
export interface FilterOption {
  /** The value to be used in filter queries */
  value: string
  /** Human-readable label for display */
  label: string
  /** Number of products matching this option */
  count: number
  /** Optional color code for color swatches (hex format) */
  colorCode?: string
}

/**
 * A filter facet group containing multiple options
 */
export interface FilterFacet {
  /** Unique code identifying this facet (e.g., 'color', 'size', 'brand') */
  code: string
  /** Display name for the facet */
  name: string
  /** Type of filter UI to render */
  type: 'checkbox' | 'color' | 'price' | 'range'
  /** Available options for this facet */
  options: FilterOption[]
}

// ============================================================================
// Applied Filter Types
// ============================================================================

/**
 * Represents an active filter that has been applied
 */
export interface AppliedFilter {
  /** Type of filter (e.g., 'checkbox', 'color', 'price') */
  type: string
  /** Code identifying the facet (e.g., 'color', 'size') */
  code: string
  /** The actual filter value */
  value: string
  /** Human-readable label for display in chips */
  label: string
}

// ============================================================================
// Price Range Types
// ============================================================================

/**
 * Price range filter values
 */
export interface PriceRange {
  min?: number
  max?: number
}

// ============================================================================
// Filter State Types
// ============================================================================

/**
 * Complete filter state for a category/search
 */
export interface FilterState {
  /** Selected values for each facet code */
  facets: Record<string, string[]>
  /** Price range filter */
  priceRange?: PriceRange
  /** Sort option */
  sortBy?: string
  /** Sort direction */
  sortOrder?: 'asc' | 'desc'
}

/**
 * Available filters response from API
 */
export interface AvailableFilters {
  /** List of available facets */
  facets: FilterFacet[]
  /** Minimum price in the result set */
  minPrice: number
  /** Maximum price in the result set */
  maxPrice: number
  /** Currency code */
  currency: string
}
