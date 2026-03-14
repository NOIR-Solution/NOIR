import React, { useState, useRef, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Check, ChevronDown, ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
import { Button } from '../button/Button';
import { Popover, PopoverContent, PopoverTrigger } from '../popover/Popover';
import { cn } from '@/lib/utils';
import { getPaginationRange } from '@/lib/utils/pagination';

// ─── Page Size Selector ──────────────────────────────────────────────────────

const STANDARD_SIZES = [10, 20, 50, 100] as const;
const MAX_CUSTOM_SIZE = 500;

interface PageSizeSelectorProps {
  pageSize: number;
  defaultPageSize?: number;
  pageSizeOptions?: number[];
  onPageSizeChange: (size: number) => void;
}

const PageSizeSelector = ({
  pageSize,
  defaultPageSize,
  pageSizeOptions,
  onPageSizeChange,
}: PageSizeSelectorProps) => {
  const { t } = useTranslation('common');
  const [open, setOpen] = useState(false);
  const [customValue, setCustomValue] = useState('');
  const [showCustomInput, setShowCustomInput] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const sizes = pageSizeOptions ?? [...STANDARD_SIZES];
  const isCustom = defaultPageSize
    ? pageSize !== defaultPageSize && !sizes.includes(pageSize)
    : !sizes.includes(pageSize);

  useEffect(() => {
    if (showCustomInput && inputRef.current) {
      inputRef.current.focus();
    }
  }, [showCustomInput]);

  const handleSelect = (size: number) => {
    onPageSizeChange(size);
    setShowCustomInput(false);
    setOpen(false);
  };

  const handleCustomSubmit = () => {
    const num = parseInt(customValue, 10);
    if (!isNaN(num) && num >= 1 && num <= MAX_CUSTOM_SIZE) {
      onPageSizeChange(num);
      setCustomValue('');
      setShowCustomInput(false);
      setOpen(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    setOpen(isOpen);
    if (!isOpen) {
      setShowCustomInput(false);
      setCustomValue('');
    }
  };

  // Display label for the trigger
  const triggerLabel = defaultPageSize && pageSize === defaultPageSize
    ? t('labels.pageSizeDefault', 'Default ({{size}})', { size: defaultPageSize })
    : isCustom
      ? t('labels.pageSizeCustom', 'Custom ({{size}})', { size: pageSize })
      : String(pageSize);

  return (
    <Popover open={open} onOpenChange={handleOpenChange}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          className="h-8 gap-1 cursor-pointer text-xs font-normal px-2.5"
          aria-label={t('labels.rowsPerPage', 'Rows per page')}
        >
          {triggerLabel}
          <ChevronDown className="h-3.5 w-3.5 opacity-50" />
        </Button>
      </PopoverTrigger>

      <PopoverContent align="start" sideOffset={4} className="w-[180px] p-0">
        {/* Default option */}
        {defaultPageSize != null && (
          <>
            <div className="py-1">
              <button
                type="button"
                className={cn(
                  'flex w-full cursor-pointer items-center gap-2 px-3 py-1.5 text-sm transition-colors hover:bg-accent',
                  pageSize === defaultPageSize && 'text-primary',
                )}
                onClick={() => handleSelect(defaultPageSize)}
              >
                <Check className={cn('h-3.5 w-3.5 shrink-0', pageSize === defaultPageSize ? 'opacity-100' : 'opacity-0')} />
                <span>{t('labels.pageSizeDefaultOption', 'Default ({{size}})', { size: defaultPageSize })}</span>
              </button>
            </div>
            <div className="border-t" />
          </>
        )}

        {/* Standard sizes */}
        <div className="py-1">
          {sizes.map((size) => {
            // Skip if it's the same as default (already shown above)
            if (defaultPageSize != null && size === defaultPageSize) return null;
            return (
              <button
                key={size}
                type="button"
                className={cn(
                  'flex w-full cursor-pointer items-center gap-2 px-3 py-1.5 text-sm transition-colors hover:bg-accent',
                  pageSize === size && 'text-primary',
                )}
                onClick={() => handleSelect(size)}
              >
                <Check className={cn('h-3.5 w-3.5 shrink-0', pageSize === size ? 'opacity-100' : 'opacity-0')} />
                <span>{size}</span>
              </button>
            );
          })}
        </div>

        {/* Custom input */}
        <div className="border-t" />
        <div className="p-2">
          {showCustomInput ? (
            <form
              onSubmit={(e) => {
                e.preventDefault();
                handleCustomSubmit();
              }}
              className="flex items-center gap-1.5"
            >
              <input
                ref={inputRef}
                type="number"
                min={1}
                max={MAX_CUSTOM_SIZE}
                value={customValue}
                onChange={(e) => setCustomValue(e.target.value)}
                placeholder={`1–${MAX_CUSTOM_SIZE}`}
                className="h-7 w-full rounded-md border border-input bg-transparent px-2 text-sm outline-none focus:ring-1 focus:ring-ring"
                aria-label={t('labels.customPageSize', 'Custom page size')}
              />
              <Button
                type="submit"
                size="sm"
                className="h-7 px-2.5 cursor-pointer text-xs"
                disabled={!customValue || parseInt(customValue, 10) < 1 || parseInt(customValue, 10) > MAX_CUSTOM_SIZE}
              >
                {t('buttons.apply', 'Apply')}
              </Button>
            </form>
          ) : (
            <button
              type="button"
              className={cn(
                'flex w-full cursor-pointer items-center gap-2 rounded-sm px-1 py-1 text-sm text-muted-foreground transition-colors hover:text-foreground',
                isCustom && 'text-primary',
              )}
              onClick={() => setShowCustomInput(true)}
            >
              <Check className={cn('h-3.5 w-3.5 shrink-0', isCustom ? 'opacity-100' : 'opacity-0')} />
              <span>
                {isCustom
                  ? t('labels.pageSizeCustomActive', 'Custom ({{size}})', { size: pageSize })
                  : t('labels.pageSizeCustomOption', 'Custom...')}
              </span>
            </button>
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
};

// ─── Main Pagination ─────────────────────────────────────────────────────────

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  totalItems: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (pageSize: number) => void;
  pageSizeOptions?: number[];
  showPageSizeSelector?: boolean;
  /** The page's default page size — shows "Default (X)" option in selector */
  defaultPageSize?: number;
  className?: string;
}

const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  totalItems,
  pageSize,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 20, 50, 100],
  showPageSizeSelector = true,
  defaultPageSize,
  className,
}) => {
  const { t } = useTranslation('common');
  const getPageNumbers = () => {
    const pages: (number | string)[] = [];
    const maxVisiblePages = 7;

    if (totalPages <= maxVisiblePages) {
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      if (currentPage <= 3) {
        for (let i = 1; i <= 4; i++) {
          pages.push(i);
        }
        pages.push('ellipsis-end');
        pages.push(totalPages);
      } else if (currentPage >= totalPages - 2) {
        pages.push(1);
        pages.push('ellipsis-start');
        for (let i = totalPages - 3; i <= totalPages; i++) {
          pages.push(i);
        }
      } else {
        pages.push(1);
        pages.push('ellipsis-start');
        for (let i = currentPage - 1; i <= currentPage + 1; i++) {
          pages.push(i);
        }
        pages.push('ellipsis-end');
        pages.push(totalPages);
      }
    }

    return pages;
  };

  const { from: startItem, to: endItem } = getPaginationRange(currentPage, pageSize, totalItems);

  const handlePageChange = (page: number) => {
    if (page >= 1 && page <= totalPages && page !== currentPage) {
      onPageChange(page);
    }
  };

  if (totalPages <= 0) {
    return null;
  }

  return (
    <div className={cn("flex flex-col sm:flex-row items-center justify-between gap-4 w-full", className)}>
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <span>
          {t('labels.showingOfItems', { from: startItem, to: endItem, total: totalItems })}
        </span>
        {showPageSizeSelector && onPageSizeChange && (
          <div className="flex items-center gap-2 ml-4">
            <span className="text-sm text-muted-foreground">{t('labels.rowsPerPage', 'Rows per page:')}</span>
            <PageSizeSelector
              pageSize={pageSize}
              defaultPageSize={defaultPageSize}
              pageSizeOptions={pageSizeOptions}
              onPageSizeChange={onPageSizeChange}
            />
          </div>
        )}
      </div>

      <div className="flex items-center gap-1">
        <Button
          variant="outline"
          size="icon"
          className="h-8 w-8 cursor-pointer"
          onClick={() => handlePageChange(1)}
          disabled={currentPage === 1}
          aria-label={t('labels.goToFirstPage', 'Go to first page')}
        >
          <ChevronsLeft className="h-4 w-4" />
        </Button>
        <Button
          variant="outline"
          size="icon"
          className="h-8 w-8 cursor-pointer"
          onClick={() => handlePageChange(currentPage - 1)}
          disabled={currentPage === 1}
          aria-label={t('labels.goToPreviousPage', 'Go to previous page')}
        >
          <ChevronLeft className="h-4 w-4" />
        </Button>

        <div className="flex items-center gap-1">
          {getPageNumbers().map((page, index) => {
            if (typeof page === 'string') {
              return (
                <div
                  key={`${page}-${index}`}
                  className="h-8 w-8 flex items-center justify-center text-muted-foreground"
                >
                  ...
                </div>
              );
            }

            return (
              <Button
                key={page}
                variant={currentPage === page ? 'default' : 'outline'}
                size="icon"
                className="h-8 w-8 cursor-pointer"
                onClick={() => handlePageChange(page)}
                aria-label={currentPage === page ? t('labels.currentPageNumber', { page, defaultValue: 'Current page, page {{page}}' }) : t('labels.goToPageNumber', { page, defaultValue: 'Go to page {{page}}' })}
                aria-current={currentPage === page ? 'page' : undefined}
              >
                {page}
              </Button>
            );
          })}
        </div>

        <Button
          variant="outline"
          size="icon"
          className="h-8 w-8 cursor-pointer"
          onClick={() => handlePageChange(currentPage + 1)}
          disabled={currentPage === totalPages}
          aria-label={t('labels.goToNextPage', 'Go to next page')}
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
        <Button
          variant="outline"
          size="icon"
          className="h-8 w-8 cursor-pointer"
          onClick={() => handlePageChange(totalPages)}
          disabled={currentPage === totalPages}
          aria-label={t('labels.goToLastPage', 'Go to last page')}
        >
          <ChevronsRight className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
};

export { Pagination };
export type { PaginationProps };
