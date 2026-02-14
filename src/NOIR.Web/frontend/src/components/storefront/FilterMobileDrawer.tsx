import * as React from 'react'
import { SlidersHorizontal } from 'lucide-react'
import {
  Button,
  ScrollArea,
  Sheet,
  SheetClose,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@uikit'

import { cn } from '@/lib/utils'

export interface FilterMobileDrawerProps {
  /** Whether the drawer is open */
  open: boolean
  /** Callback when open state changes */
  onOpenChange: (open: boolean) => void
  /** Filter content to render inside the drawer */
  children: React.ReactNode
  /** Number of active filters (optional, for badge) */
  activeFilterCount?: number
  /** Optional title for the drawer */
  title?: string
  /** Optional description for the drawer */
  description?: string
}

/**
 * Mobile drawer component for filters using shadcn Sheet
 * Shows FilterSidebar content in a slide-out drawer on mobile
 */
export const FilterMobileDrawer = ({
  open,
  onOpenChange,
  children,
  activeFilterCount = 0,
  title = 'Filters',
  description = 'Refine your search results',
}: FilterMobileDrawerProps) => {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="left" className="w-full sm:max-w-md p-0 flex flex-col">
        <SheetHeader className="px-4 pt-4 pb-2 border-b">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <SlidersHorizontal className="size-5" aria-hidden="true" />
              <SheetTitle>{title}</SheetTitle>
              {activeFilterCount > 0 && (
                <span className="ml-1 rounded-full bg-primary text-primary-foreground text-xs font-medium px-2 py-0.5">
                  {activeFilterCount}
                </span>
              )}
            </div>
          </div>
          <SheetDescription className="text-left text-sm">
            {description}
          </SheetDescription>
        </SheetHeader>

        <ScrollArea className="flex-1 px-4 py-4">
          {children}
        </ScrollArea>

        <div className="border-t p-4">
          <SheetClose asChild>
            <Button className="w-full">
              View Results
            </Button>
          </SheetClose>
        </div>
      </SheetContent>
    </Sheet>
  )
}

/**
 * Trigger button for opening the mobile filter drawer
 */
export interface FilterMobileTriggerProps {
  /** Callback when button is clicked */
  onClick: () => void
  /** Number of active filters (optional, for badge) */
  activeFilterCount?: number
  /** Optional className */
  className?: string
}

export const FilterMobileTrigger = ({
  onClick,
  activeFilterCount = 0,
  className,
}: FilterMobileTriggerProps) => {
  return (
    <Button
      type="button"
      variant="outline"
      size="sm"
      onClick={onClick}
      className={cn('lg:hidden gap-2', className)}
      aria-label="Open filters"
    >
      <SlidersHorizontal className="size-4" aria-hidden="true" />
      <span>Filters</span>
      {activeFilterCount > 0 && (
        <span className="rounded-full bg-primary text-primary-foreground text-xs font-medium px-1.5 py-0.5 min-w-5 text-center">
          {activeFilterCount}
        </span>
      )}
    </Button>
  )
}
