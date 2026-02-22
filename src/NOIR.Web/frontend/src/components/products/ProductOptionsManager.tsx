/**
 * ProductOptionsManager Component
 *
 * Manages product options (Color, Size, Material, etc.) and their values.
 * Supports adding, editing, and removing options with color swatches.
 */
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Plus,
  Trash2,
  Palette,
  ChevronDown,
  ChevronRight,
  GripVertical,
  X,
} from 'lucide-react'
import {
  Button,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  Input,
} from '@uikit'

import { sanitizeColorCode } from '@/lib/color-utils'
import type {
  ProductOption,
  ProductOptionValue,
  AddProductOptionRequest,
  AddProductOptionValueRequest,
} from '@/types/product'

interface ProductOptionsManagerProps {
  productId: string
  options: ProductOption[]
  onAddOption: (request: AddProductOptionRequest) => Promise<void>
  onUpdateOption: (
    optionId: string,
    request: { name: string; displayName?: string; sortOrder: number }
  ) => Promise<void>
  onDeleteOption: (optionId: string) => Promise<void>
  onAddValue: (optionId: string, request: AddProductOptionValueRequest) => Promise<void>
  onUpdateValue: (
    optionId: string,
    valueId: string,
    request: {
      value: string
      displayValue?: string
      colorCode?: string
      swatchUrl?: string
      sortOrder: number
    }
  ) => Promise<void>
  onDeleteValue: (optionId: string, valueId: string) => Promise<void>
  disabled?: boolean
}

export const ProductOptionsManager = ({
  options,
  onAddOption,
  onDeleteOption,
  onAddValue,
  onUpdateValue,
  onDeleteValue,
  disabled = false,
}: ProductOptionsManagerProps) => {
  const { t } = useTranslation()
  const [expandedOptions, setExpandedOptions] = useState<Set<string>>(new Set())
  const [newOptionName, setNewOptionName] = useState('')
  const [newValueInputs, setNewValueInputs] = useState<Record<string, string>>({})
  const [deleteTarget, setDeleteTarget] = useState<{
    type: 'option' | 'value'
    optionId: string
    valueId?: string
    name: string
  } | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const toggleOption = (optionId: string) => {
    const next = new Set(expandedOptions)
    if (next.has(optionId)) {
      next.delete(optionId)
    } else {
      next.add(optionId)
    }
    setExpandedOptions(next)
  }

  const handleAddOption = async () => {
    if (!newOptionName.trim() || isSubmitting) return

    setIsSubmitting(true)
    try {
      await onAddOption({
        name: newOptionName.trim(),
        displayName: newOptionName.trim(),
        sortOrder: options.length,
        values: [],
      })
      setNewOptionName('')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleAddValue = async (optionId: string) => {
    const value = newValueInputs[optionId]?.trim()
    if (!value || isSubmitting) return

    setIsSubmitting(true)
    try {
      const option = options.find((o) => o.id === optionId)
      await onAddValue(optionId, {
        value,
        displayValue: value,
        sortOrder: option?.values.length ?? 0,
      })
      setNewValueInputs((prev) => ({ ...prev, [optionId]: '' }))
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleConfirmDelete = async () => {
    if (!deleteTarget || isSubmitting) return

    setIsSubmitting(true)
    try {
      if (deleteTarget.type === 'option') {
        await onDeleteOption(deleteTarget.optionId)
      } else if (deleteTarget.valueId) {
        await onDeleteValue(deleteTarget.optionId, deleteTarget.valueId)
      }
    } finally {
      setIsSubmitting(false)
      setDeleteTarget(null)
    }
  }

  const handleColorChange = async (
    optionId: string,
    valueId: string,
    value: ProductOptionValue,
    colorCode: string
  ) => {
    await onUpdateValue(optionId, valueId, {
      value: value.value,
      displayValue: value.displayValue ?? value.value,
      colorCode,
      swatchUrl: value.swatchUrl ?? undefined,
      sortOrder: value.sortOrder,
    })
  }

  return (
    <div className="space-y-4">
      {/* Option List */}
      <div className="space-y-2">
        {options.map((option) => (
          <Collapsible
            key={option.id}
            open={expandedOptions.has(option.id)}
            onOpenChange={() => toggleOption(option.id)}
          >
            <div className="rounded-lg border bg-card">
              {/* Option Header */}
              <div className="flex items-center gap-2 p-3">
                <GripVertical className="h-4 w-4 text-muted-foreground cursor-grab" />
                <CollapsibleTrigger asChild>
                  <Button variant="ghost" size="sm" className="p-0 h-auto">
                    {expandedOptions.has(option.id) ? (
                      <ChevronDown className="h-4 w-4" />
                    ) : (
                      <ChevronRight className="h-4 w-4" />
                    )}
                  </Button>
                </CollapsibleTrigger>
                <span className="font-medium flex-1">
                  {option.displayName || option.name}
                </span>
                <span className="text-sm text-muted-foreground">
                  {option.values.length} {t('products.options.values')}
                </span>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-8 w-8 text-destructive hover:text-destructive cursor-pointer"
                  onClick={() =>
                    setDeleteTarget({
                      type: 'option',
                      optionId: option.id,
                      name: option.displayName || option.name,
                    })
                  }
                  disabled={disabled}
                  aria-label={t('products.options.deleteOption', {
                    name: option.displayName || option.name,
                  })}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>

              {/* Option Values */}
              <CollapsibleContent>
                <div className="border-t p-3 space-y-3">
                  {/* Existing Values */}
                  <div className="flex flex-wrap gap-2">
                    {option.values.map((value) => (
                      <div
                        key={value.id}
                        className="flex items-center gap-2 rounded-md border bg-muted/50 px-2 py-1"
                      >
                        {/* Color Swatch */}
                        {value.colorCode && (
                          <div
                            className="h-4 w-4 rounded-full border"
                            style={{ backgroundColor: sanitizeColorCode(value.colorCode) }}
                          />
                        )}
                        <span className="text-sm">{value.displayValue || value.value}</span>
                        {/* Color Picker */}
                        <label className="cursor-pointer">
                          <Palette className="h-3.5 w-3.5 text-muted-foreground hover:text-foreground" />
                          <input
                            type="color"
                            className="sr-only"
                            value={sanitizeColorCode(value.colorCode, '#000000')}
                            onChange={(e) =>
                              handleColorChange(option.id, value.id, value, e.target.value)
                            }
                            disabled={disabled}
                            aria-label={t('products.options.changeColor', {
                              value: value.displayValue || value.value,
                            })}
                          />
                        </label>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-5 w-5 hover:bg-destructive/10 cursor-pointer"
                          onClick={() =>
                            setDeleteTarget({
                              type: 'value',
                              optionId: option.id,
                              valueId: value.id,
                              name: value.displayValue || value.value,
                            })
                          }
                          disabled={disabled}
                          aria-label={t('products.options.deleteValue', {
                            name: value.displayValue || value.value,
                          })}
                        >
                          <X className="h-3 w-3" />
                        </Button>
                      </div>
                    ))}
                  </div>

                  {/* Add Value Input */}
                  <div className="flex gap-2">
                    <Input
                      placeholder={t('products.options.addValuePlaceholder')}
                      value={newValueInputs[option.id] || ''}
                      onChange={(e) =>
                        setNewValueInputs((prev) => ({
                          ...prev,
                          [option.id]: e.target.value,
                        }))
                      }
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                          e.preventDefault()
                          handleAddValue(option.id)
                        }
                      }}
                      disabled={disabled || isSubmitting}
                      className="flex-1"
                    />
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => handleAddValue(option.id)}
                      disabled={disabled || isSubmitting || !newValueInputs[option.id]?.trim()}
                      className="cursor-pointer"
                    >
                      <Plus className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CollapsibleContent>
            </div>
          </Collapsible>
        ))}
      </div>

      {/* Add New Option */}
      <div className="flex gap-2">
        <Input
          placeholder={t('products.options.addOptionPlaceholder')}
          value={newOptionName}
          onChange={(e) => setNewOptionName(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault()
              handleAddOption()
            }
          }}
          disabled={disabled || isSubmitting}
          className="flex-1"
        />
        <Button
          variant="secondary"
          onClick={handleAddOption}
          disabled={disabled || isSubmitting || !newOptionName.trim()}
          className="cursor-pointer"
        >
          <Plus className="h-4 w-4 mr-2" />
          {t('products.options.addOption')}
        </Button>
      </div>

      {/* Delete Confirmation Dialog */}
      <Credenza open={!!deleteTarget} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>
                  {deleteTarget?.type === 'option'
                    ? t('products.options.confirmDeleteOption')
                    : t('products.options.confirmDeleteValue')}
                </CredenzaTitle>
                <CredenzaDescription>
                  {deleteTarget?.type === 'option'
                    ? t('products.options.deleteOptionWarning', { name: deleteTarget?.name })
                    : t('products.options.deleteValueWarning', { name: deleteTarget?.name })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)} className="cursor-pointer">
              {t('buttons.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleConfirmDelete}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {t('buttons.delete')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}
