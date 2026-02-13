import * as React from 'react'
import { ChevronDown } from 'lucide-react'
import { Collapsible, CollapsibleContent, CollapsibleTrigger, Separator, Skeleton } from '@uikit'

import { cn } from '@/lib/utils'
import { FacetCheckbox, FacetColorSwatch, FacetPriceRange } from './facets'
import type {
  FilterFacet,
  AppliedFilter,
  FilterState,
  AvailableFilters,
  PriceRange,
} from '@/types/filter'

export interface FilterSidebarProps {
  /** Category slug for filtering (optional) */
  categorySlug?: string
  /** Callback when filters change */
  onFilterChange: (filterState: FilterState) => void
  /** Currently applied filters */
  appliedFilters: AppliedFilter[]
  /** Available filters (from API or placeholder) */
  availableFilters?: AvailableFilters | null
  /** Whether filters are loading */
  isLoading?: boolean
  /** Currency for price display */
  currency?: string
  /** Optional className for the container */
  className?: string
}

// Placeholder data for development (will be replaced with API data)
const PLACEHOLDER_FILTERS: AvailableFilters = {
  facets: [
    {
      code: 'category',
      name: 'Category',
      type: 'checkbox',
      options: [
        { value: 'electronics', label: 'Electronics', count: 42 },
        { value: 'clothing', label: 'Clothing', count: 38 },
        { value: 'home', label: 'Home & Garden', count: 25 },
        { value: 'sports', label: 'Sports', count: 18 },
        { value: 'toys', label: 'Toys & Games', count: 12 },
        { value: 'books', label: 'Books', count: 45 },
      ],
    },
    {
      code: 'brand',
      name: 'Brand',
      type: 'checkbox',
      options: [
        { value: 'apple', label: 'Apple', count: 15 },
        { value: 'samsung', label: 'Samsung', count: 12 },
        { value: 'sony', label: 'Sony', count: 8 },
        { value: 'nike', label: 'Nike', count: 20 },
        { value: 'adidas', label: 'Adidas', count: 18 },
      ],
    },
    {
      code: 'color',
      name: 'Color',
      type: 'color',
      options: [
        { value: 'black', label: 'Black', count: 45, colorCode: '#000000' },
        { value: 'white', label: 'White', count: 38, colorCode: '#FFFFFF' },
        { value: 'red', label: 'Red', count: 22, colorCode: '#EF4444' },
        { value: 'blue', label: 'Blue', count: 30, colorCode: '#3B82F6' },
        { value: 'green', label: 'Green', count: 18, colorCode: '#22C55E' },
        { value: 'yellow', label: 'Yellow', count: 12, colorCode: '#EAB308' },
        { value: 'purple', label: 'Purple', count: 15, colorCode: '#A855F7' },
        { value: 'orange', label: 'Orange', count: 10, colorCode: '#F97316' },
      ],
    },
    {
      code: 'size',
      name: 'Size',
      type: 'checkbox',
      options: [
        { value: 'xs', label: 'XS', count: 8 },
        { value: 's', label: 'S', count: 15 },
        { value: 'm', label: 'M', count: 22 },
        { value: 'l', label: 'L', count: 18 },
        { value: 'xl', label: 'XL', count: 12 },
        { value: 'xxl', label: 'XXL', count: 5 },
      ],
    },
  ],
  minPrice: 0,
  maxPrice: 1000,
  currency: 'USD',
}

/**
 * Main filter sidebar component for storefront
 * Contains collapsible sections for different filter types
 */
export function FilterSidebar({
  onFilterChange,
  appliedFilters,
  availableFilters,
  isLoading = false,
  currency = '$',
  className,
}: FilterSidebarProps) {
  // Use placeholder data if no available filters provided
  const filters = availableFilters || PLACEHOLDER_FILTERS

  // Track open state for each collapsible section
  const [openSections, setOpenSections] = React.useState<Set<string>>(
    () => new Set(filters.facets.map((f) => f.code))
  )

  // Track local filter state
  const [filterState, setFilterState] = React.useState<FilterState>({
    facets: {},
    priceRange: undefined,
  })

  // Initialize filter state from applied filters
  React.useEffect(() => {
    const facets: Record<string, string[]> = {}
    let priceRange: PriceRange | undefined

    for (const filter of appliedFilters) {
      if (filter.type === 'price') {
        if (filter.code === 'min') {
          priceRange = { ...priceRange, min: parseFloat(filter.value) }
        } else if (filter.code === 'max') {
          priceRange = { ...priceRange, max: parseFloat(filter.value) }
        }
      } else {
        if (!facets[filter.code]) {
          facets[filter.code] = []
        }
        facets[filter.code].push(filter.value)
      }
    }

    setFilterState({ facets, priceRange })
  }, [appliedFilters])

  const toggleSection = (code: string) => {
    setOpenSections((prev) => {
      const next = new Set(prev)
      if (next.has(code)) {
        next.delete(code)
      } else {
        next.add(code)
      }
      return next
    })
  }

  const handleFacetChange = (code: string, values: string[]) => {
    const newState: FilterState = {
      ...filterState,
      facets: {
        ...filterState.facets,
        [code]: values,
      },
    }
    setFilterState(newState)
    onFilterChange(newState)
  }

  const handlePriceChange = (min?: number, max?: number) => {
    const newState: FilterState = {
      ...filterState,
      priceRange: min !== undefined || max !== undefined ? { min, max } : undefined,
    }
    setFilterState(newState)
    onFilterChange(newState)
  }

  const renderFacet = (facet: FilterFacet) => {
    const selectedValues = filterState.facets[facet.code] || []

    switch (facet.type) {
      case 'color':
        return (
          <FacetColorSwatch
            name={facet.name}
            options={facet.options}
            selectedValues={selectedValues}
            onChange={(values) => handleFacetChange(facet.code, values)}
          />
        )
      case 'checkbox':
      default:
        return (
          <FacetCheckbox
            name={facet.name}
            options={facet.options}
            selectedValues={selectedValues}
            onChange={(values) => handleFacetChange(facet.code, values)}
          />
        )
    }
  }

  if (isLoading) {
    return (
      <div className={cn('space-y-6', className)}>
        {[1, 2, 3].map((i) => (
          <div key={i} className="space-y-3">
            <Skeleton className="h-5 w-24" />
            <div className="space-y-2">
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-4 w-5/6" />
            </div>
          </div>
        ))}
      </div>
    )
  }

  return (
    <aside
      className={cn('space-y-4', className)}
      role="complementary"
      aria-label="Product filters"
    >
      <h3 className="font-semibold text-lg">Filters</h3>

      {/* Price Range - Always show first */}
      <Collapsible
        open={openSections.has('price')}
        onOpenChange={() => toggleSection('price')}
      >
        <CollapsibleTrigger className="flex w-full items-center justify-between py-2 text-sm font-medium hover:text-primary transition-colors cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 rounded-sm">
          <span>Price</span>
          <ChevronDown
            className={cn(
              'size-4 transition-transform duration-200',
              openSections.has('price') && 'rotate-180'
            )}
            aria-hidden="true"
          />
        </CollapsibleTrigger>
        <CollapsibleContent className="pt-2">
          <FacetPriceRange
            min={filters.minPrice}
            max={filters.maxPrice}
            selectedMin={filterState.priceRange?.min}
            selectedMax={filterState.priceRange?.max}
            onChange={handlePriceChange}
            currency={currency}
          />
        </CollapsibleContent>
      </Collapsible>

      <Separator />

      {/* Dynamic Facets */}
      {filters.facets.map((facet, index) => (
        <React.Fragment key={facet.code}>
          <Collapsible
            open={openSections.has(facet.code)}
            onOpenChange={() => toggleSection(facet.code)}
          >
            <CollapsibleTrigger className="flex w-full items-center justify-between py-2 text-sm font-medium hover:text-primary transition-colors cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 rounded-sm">
              <span>{facet.name}</span>
              <div className="flex items-center gap-2">
                {(filterState.facets[facet.code]?.length ?? 0) > 0 && (
                  <span className="text-xs text-muted-foreground">
                    ({filterState.facets[facet.code].length})
                  </span>
                )}
                <ChevronDown
                  className={cn(
                    'size-4 transition-transform duration-200',
                    openSections.has(facet.code) && 'rotate-180'
                  )}
                  aria-hidden="true"
                />
              </div>
            </CollapsibleTrigger>
            <CollapsibleContent className="pt-2">
              {renderFacet(facet)}
            </CollapsibleContent>
          </Collapsible>
          {index < filters.facets.length - 1 && <Separator />}
        </React.Fragment>
      ))}
    </aside>
  )
}
