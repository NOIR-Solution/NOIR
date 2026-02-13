/**
 * ProductAttributesSection - Dynamic attribute form for product editing
 * Phase 9: Product Form Attribute Integration
 *
 * Renders a card with attribute inputs based on the product's category.
 * Uses the form schema API to get the list of attributes and their current values.
 */
import { useState, useEffect, useCallback, useMemo, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Package, AlertCircle, ChevronDown, ChevronUp, FolderOpen } from 'lucide-react'
import {
  Alert,
  AlertDescription,
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  Skeleton,
} from '@uikit'

import { useProductAttributeForm } from '@/hooks/useProductAttributes'
import { AttributeInputFactory } from './AttributeInputs'
import type { AttributeValue } from './AttributeInputs'
import type { ProductAttributeFormField } from '@/types/productAttribute'

export interface ProductAttributesSectionProps {
  /** The product ID to fetch attributes for */
  productId: string
  /** The current category ID (used to detect category changes) */
  categoryId: string | null
  /** Whether the form is in view-only mode */
  isViewMode: boolean
  /** Callback when any attribute value changes */
  onAttributesChange?: (values: Record<string, unknown>) => void
  /** Optional variant ID for variant-specific attributes */
  variantId?: string
}

/**
 * ProductAttributesSection displays a dynamic form for product attributes
 * based on the product's category. Attributes are loaded from the form schema API.
 */
export function ProductAttributesSection({
  productId,
  categoryId,
  isViewMode,
  onAttributesChange,
  variantId,
}: ProductAttributesSectionProps) {
  const { t } = useTranslation('common')
  const [isOpen, setIsOpen] = useState(true)
  const [localValues, setLocalValues] = useState<Record<string, unknown>>({})
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false)
  const [showCategoryChangeDialog, setShowCategoryChangeDialog] = useState(false)
  const previousCategoryIdRef = useRef<string | null>(categoryId)
  const pendingCategoryIdRef = useRef<string | null>(null)

  // Fetch form schema with current values
  const { data: formSchema, loading, error, refresh } = useProductAttributeForm(productId, variantId)

  // Initialize local values from form schema
  useEffect(() => {
    if (formSchema?.fields) {
      const initialValues: Record<string, unknown> = {}
      formSchema.fields.forEach((field) => {
        initialValues[field.attributeId] = field.currentValue ?? field.defaultValue ?? null
      })
      setLocalValues(initialValues)
      setHasUnsavedChanges(false)
    }
  }, [formSchema])

  // Handle category change confirmation
  const handleConfirmCategoryChange = useCallback(() => {
    setShowCategoryChangeDialog(false)
    previousCategoryIdRef.current = pendingCategoryIdRef.current
    if (pendingCategoryIdRef.current !== undefined) {
      refresh()
    }
  }, [refresh])

  // Refresh when category changes - warn if unsaved changes exist
  useEffect(() => {
    // Skip on initial mount
    if (previousCategoryIdRef.current === categoryId) {
      return
    }

    // Check if category actually changed (not just mount)
    const categoryChanged = previousCategoryIdRef.current !== null && categoryId !== previousCategoryIdRef.current

    if (categoryChanged && hasUnsavedChanges && Object.keys(localValues).length > 0) {
      // Store pending category and show confirmation dialog
      pendingCategoryIdRef.current = categoryId
      setShowCategoryChangeDialog(true)
      return
    }

    // Update ref and refresh
    previousCategoryIdRef.current = categoryId
    if (categoryId !== undefined) {
      refresh()
    }
  }, [categoryId, refresh, hasUnsavedChanges, localValues])

  // Handle value change for a single attribute
  const handleValueChange = useCallback(
    (attributeId: string, value: AttributeValue) => {
      setLocalValues((prev) => {
        const updated = { ...prev, [attributeId]: value }
        setHasUnsavedChanges(true)
        // Notify parent of changes
        if (onAttributesChange) {
          onAttributesChange(updated)
        }
        return updated
      })
    },
    [onAttributesChange]
  )

  // Get validation errors
  const validationErrors = useMemo(() => {
    const errors: Record<string, string> = {}
    if (!formSchema?.fields) return errors

    formSchema.fields.forEach((field) => {
      if (field.isRequired) {
        const value = localValues[field.attributeId]
        if (value === null || value === undefined || value === '' ||
            (Array.isArray(value) && value.length === 0)) {
          errors[field.attributeId] = t('validation.required', { field: field.name })
        }
      }
    })
    return errors
  }, [formSchema, localValues, t])

  // Count required fields that are missing
  const missingRequiredCount = Object.keys(validationErrors).length

  // Loading state
  if (loading) {
    return (
      <Card className="shadow-sm">
        <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
          <div className="flex items-center gap-3">
            <Skeleton className="h-6 w-6 rounded" />
            <Skeleton className="h-6 w-40" />
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="space-y-2">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-10 w-full" />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    )
  }

  // Error state
  if (error) {
    return (
      <Card className="shadow-sm border-destructive/30">
        <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
          <CardTitle className="flex items-center gap-2 text-destructive">
            <AlertCircle className="h-5 w-5" />
            {t('products.attributes.loadError')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
          <Button
            variant="outline"
            size="sm"
            onClick={() => refresh()}
            className="mt-4 cursor-pointer"
          >
            {t('buttons.retry')}
          </Button>
        </CardContent>
      </Card>
    )
  }

  // Empty state - no category or no attributes
  if (!formSchema || !formSchema.fields || formSchema.fields.length === 0) {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
          <CardTitle className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-muted">
              <Package className="h-4 w-4 text-muted-foreground" />
            </div>
            {t('products.attributes.title')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-8 text-center">
            <div className="p-3 rounded-xl bg-muted/50 mb-3">
              <FolderOpen className="h-8 w-8 text-muted-foreground" />
            </div>
            <p className="text-muted-foreground">
              {categoryId
                ? t('products.attributes.noAttributesForCategory')
                : t('products.attributes.selectCategoryFirst')}
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  // Main render with attributes
  return (
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
      <Collapsible open={isOpen} onOpenChange={setIsOpen}>
        <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-primary/20 to-primary/10 shadow-sm">
                <Package className="h-4 w-4 text-primary" />
              </div>
              <div>
                <CardTitle className="flex items-center gap-2">
                  {t('products.attributes.title')}
                  {hasUnsavedChanges && !isViewMode && (
                    <span className="text-xs font-normal text-amber-500">
                      ({t('products.attributes.unsaved')})
                    </span>
                  )}
                </CardTitle>
                <CardDescription>
                  {formSchema.categoryName || t('products.attributes.general')}
                  {' · '}
                  {formSchema.fields.length} {t('products.attributes.attributeCount')}
                </CardDescription>
              </div>
            </div>
            <CollapsibleTrigger asChild>
              <Button
                variant="ghost"
                size="sm"
                className="cursor-pointer"
                aria-label={isOpen ? t('buttons.collapse') : t('buttons.expand')}
              >
                {isOpen ? (
                  <ChevronUp className="h-4 w-4" />
                ) : (
                  <ChevronDown className="h-4 w-4" />
                )}
              </Button>
            </CollapsibleTrigger>
          </div>
        </CardHeader>

        <CollapsibleContent>
          <CardContent className="pt-0">
            {/* Validation warning */}
            {missingRequiredCount > 0 && !isViewMode && (
              <Alert className="mb-4 border-amber-500/30 bg-amber-50/50 dark:bg-amber-950/20">
                <AlertCircle className="h-4 w-4 text-amber-500" />
                <AlertDescription className="text-amber-700 dark:text-amber-400">
                  {t('products.attributes.missingRequired', { count: missingRequiredCount })}
                </AlertDescription>
              </Alert>
            )}

            {/* Attribute grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {formSchema.fields.map((field) => (
                <AttributeInputFactory
                  key={field.attributeId}
                  field={field}
                  value={localValues[field.attributeId] as AttributeValue}
                  onChange={(value) => handleValueChange(field.attributeId, value)}
                  disabled={isViewMode}
                  error={!isViewMode ? validationErrors[field.attributeId] : undefined}
                />
              ))}
            </div>
          </CardContent>
        </CollapsibleContent>
      </Collapsible>

      {/* Category change confirmation dialog */}
      <AlertDialog open={showCategoryChangeDialog} onOpenChange={setShowCategoryChangeDialog}>
        <AlertDialogContent className="border-destructive/30">
          <AlertDialogHeader>
            <AlertDialogTitle className="flex items-center gap-2">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <AlertCircle className="h-5 w-5 text-destructive" />
              </div>
              {t('products.attributes.confirmCategoryChangeTitle')}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {t('products.attributes.confirmCategoryChangeDescription')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">
              {t('buttons.cancel')}
            </AlertDialogCancel>
            <AlertDialogAction
              className="cursor-pointer bg-destructive hover:bg-destructive/90"
              onClick={handleConfirmCategoryChange}
            >
              {t('buttons.continue')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </Card>
  )
}

/**
 * Hook to get the current attribute values for saving
 */
export function useAttributeValues(
  formSchema: { fields: ProductAttributeFormField[] } | null,
  localValues: Record<string, unknown>
) {
  return useMemo(() => {
    if (!formSchema?.fields) return []

    return formSchema.fields
      .filter((field) => localValues[field.attributeId] !== undefined)
      .map((field) => ({
        attributeId: field.attributeId,
        value: localValues[field.attributeId],
      }))
  }, [formSchema, localValues])
}
