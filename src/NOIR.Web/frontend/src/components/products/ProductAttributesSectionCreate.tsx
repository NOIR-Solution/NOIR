/**
 * ProductAttributesSectionCreate - Dynamic attribute form for new product creation
 *
 * Renders attribute inputs based on the selected category, without requiring a product ID.
 * Uses getCategoryAttributeFormSchema API to get the list of attributes.
 */
import { useState, useEffect, useCallback, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Package, AlertCircle, ChevronDown, ChevronUp, FolderOpen } from 'lucide-react'
import {
  Alert,
  AlertDescription,
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

import { useCategoryAttributeForm } from '@/hooks/useProductAttributes'
import { AttributeInputFactory } from './AttributeInputs'
import type { AttributeValue } from './AttributeInputs'

export interface ProductAttributesSectionCreateProps {
  /** The selected category ID */
  categoryId: string | null
  /** Whether the form is in view-only mode */
  isViewMode?: boolean
  /** Callback when any attribute value changes */
  onAttributesChange?: (values: Record<string, unknown>) => void
}

/**
 * ProductAttributesSectionCreate displays a dynamic form for product attributes
 * based on the selected category, for use in new product creation flow.
 */
export function ProductAttributesSectionCreate({
  categoryId,
  isViewMode = false,
  onAttributesChange,
}: ProductAttributesSectionCreateProps) {
  const { t } = useTranslation('common')
  const [isOpen, setIsOpen] = useState(true)
  const [localValues, setLocalValues] = useState<Record<string, unknown>>({})

  // Fetch form schema for the category
  const { data: formSchema, loading, error, refresh } = useCategoryAttributeForm(categoryId)

  // Initialize local values from form schema (with default values)
  useEffect(() => {
    if (formSchema?.fields) {
      const initialValues: Record<string, unknown> = {}
      formSchema.fields.forEach((field) => {
        initialValues[field.attributeId] = field.defaultValue ?? null
      })
      setLocalValues(initialValues)
      // Notify parent of initial values
      if (onAttributesChange) {
        onAttributesChange(initialValues)
      }
    }
  }, [formSchema]) // eslint-disable-line react-hooks/exhaustive-deps

  // Handle value change for a single attribute
  const handleValueChange = useCallback(
    (attributeId: string, value: AttributeValue) => {
      setLocalValues((prev) => {
        const updated = { ...prev, [attributeId]: value }
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
  if (!categoryId || !formSchema || !formSchema.fields || formSchema.fields.length === 0) {
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
                <CardTitle>{t('products.attributes.title')}</CardTitle>
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
    </Card>
  )
}

export default ProductAttributesSectionCreate
