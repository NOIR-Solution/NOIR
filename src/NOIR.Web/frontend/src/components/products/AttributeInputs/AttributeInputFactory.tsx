/**
 * AttributeInputFactory - Routes to correct input component by AttributeType
 * Phase 9: Product Form Attribute Integration
 */
import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useDropzone } from 'react-dropzone'
import { HelpCircle, Upload, X, FileIcon, Image, Clock, Plus, Minus } from 'lucide-react'
import {
  Button,
  Calendar,
  Checkbox,
  Input,
  Label,
  Popover,
  PopoverContent,
  PopoverTrigger,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Switch,
  Textarea,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@uikit'

import { CalendarIcon, Check, Link as LinkIcon, AlertCircle } from 'lucide-react'
import { format, parseISO } from 'date-fns'
import { cn } from '@/lib/utils'
import type { AttributeInputProps, AttributeValue } from './types'
import type { AttributeType } from '@/types/productAttribute'

/**
 * Factory component that renders the appropriate input based on attribute type
 */
export const AttributeInputFactory = ({
  field,
  value,
  onChange,
  disabled = false,
  error,
}: AttributeInputProps) => {
  const { t } = useTranslation('common')
  const type = field.type as AttributeType

  // Render the appropriate input based on type
  const renderInput = () => {
    switch (type) {
      case 'Select':
        return (
          <SelectInput
            field={field}
            value={value as string}
            onChange={onChange}
            disabled={disabled}
          />
        )

      case 'MultiSelect':
        return (
          <MultiSelectInput
            field={field}
            value={value as string[]}
            onChange={onChange}
            disabled={disabled}
          />
        )

      case 'Text':
        return (
          <TextInput
            field={field}
            value={value as string}
            onChange={onChange}
            disabled={disabled}
          />
        )

      case 'TextArea':
        return (
          <TextAreaInput
            field={field}
            value={value as string}
            onChange={onChange}
            disabled={disabled}
          />
        )

      case 'Number':
        return (
          <NumberInput
            field={field}
            value={value as number}
            onChange={onChange}
            disabled={disabled}
            isDecimal={false}
          />
        )

      case 'Decimal':
        return (
          <NumberInput
            field={field}
            value={value as number}
            onChange={onChange}
            disabled={disabled}
            isDecimal={true}
          />
        )

      case 'Boolean':
        return (
          <BooleanInput
            field={field}
            value={value as boolean}
            onChange={onChange}
            disabled={disabled}
          />
        )

      case 'Date':
        return (
          <DateInput
            field={field}
            value={value as string}
            onChange={onChange}
            disabled={disabled}
            showTime={false}
          />
        )

      case 'DateTime':
        return (
          <DateInput
            field={field}
            value={value as string}
            onChange={onChange}
            disabled={disabled}
            showTime={true}
          />
        )

      case 'Color':
        return (
          <ColorInput
            field={field}
            value={value as string}
            onChange={onChange}
            disabled={disabled}
          />
        )

      case 'Range':
        return (
          <RangeInput
            field={field}
            value={value as string}
            onChange={onChange}
            disabled={disabled}
          />
        )

      case 'Url':
        return (
          <UrlInput
            field={field}
            value={value as string}
            onChange={onChange}
            disabled={disabled}
          />
        )

      case 'File':
        return (
          <FileInput
            field={field}
            value={value as string}
            onChange={onChange}
            disabled={disabled}
          />
        )

      default:
        return (
          <TextInput
            field={field}
            value={value as string}
            onChange={onChange}
            disabled={disabled}
          />
        )
    }
  }

  return (
    <div className="space-y-2">
      {/* Label with required indicator and help tooltip */}
      <div className="flex items-center gap-2">
        <Label
          htmlFor={`attr-${field.attributeId}`}
          className="text-sm font-medium"
        >
          {field.name}
          {field.isRequired && (
            <span className="text-destructive ml-1" aria-hidden="true">*</span>
          )}
          {field.unit && (
            <span className="text-muted-foreground font-normal ml-1">
              ({field.unit})
            </span>
          )}
        </Label>
        {field.helpText && (
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <button
                  type="button"
                  className="text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
                  aria-label={t('products.attributes.helpTooltip', { name: field.name })}
                >
                  <HelpCircle className="h-4 w-4" />
                </button>
              </TooltipTrigger>
              <TooltipContent side="top" className="max-w-xs">
                <p>{field.helpText}</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}
      </div>

      {/* Input component */}
      {renderInput()}

      {/* Error message */}
      {error && (
        <p className="text-sm text-destructive flex items-center gap-1">
          <AlertCircle className="h-3 w-3" />
          {error}
        </p>
      )}
    </div>
  )
}

// ============================================================================
// Individual Input Components
// ============================================================================

interface InputComponentProps {
  field: AttributeInputProps['field']
  value: AttributeValue
  onChange: AttributeInputProps['onChange']
  disabled?: boolean
}

/** Select dropdown input */
const SelectInput = ({ field, value, onChange, disabled }: InputComponentProps) => {
  const { t } = useTranslation('common')

  return (
    <Select
      value={(value as string) || ''}
      onValueChange={(val) => onChange(val || null)}
      disabled={disabled}
    >
      <SelectTrigger
        id={`attr-${field.attributeId}`}
        className="cursor-pointer"
        aria-label={field.name}
        aria-required={field.isRequired}
      >
        <SelectValue placeholder={field.placeholder || t('products.attributes.selectPlaceholder')} />
      </SelectTrigger>
      <SelectContent>
        {field.options?.map((option) => (
          <SelectItem
            key={option.id}
            value={option.value}
            className="cursor-pointer"
          >
            {option.displayValue}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}

/** Multi-select checkbox group with responsive layout */
const MultiSelectInput = ({ field, value, onChange, disabled }: InputComponentProps) => {
  const { t } = useTranslation('common')
  const selectedValues = Array.isArray(value) ? value : []
  const optionCount = field.options?.length || 0

  const handleToggle = (optionValue: string, checked: boolean) => {
    if (checked) {
      onChange([...selectedValues, optionValue])
    } else {
      onChange(selectedValues.filter((v) => v !== optionValue))
    }
  }

  const handleSelectAll = () => {
    if (field.options) {
      onChange(field.options.map(o => o.value))
    }
  }

  const handleClearAll = () => {
    onChange([])
  }

  return (
    <div className="space-y-3">
      {/* Selection controls for many options */}
      {optionCount > 4 && (
        <div className="flex items-center justify-between text-xs">
          <span className="text-muted-foreground">
            {t('products.attributes.selectedCount', { count: selectedValues.length, total: optionCount })}
          </span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={handleSelectAll}
              disabled={disabled || selectedValues.length === optionCount}
              className={cn(
                'text-primary hover:text-primary/80 transition-colors cursor-pointer',
                (disabled || selectedValues.length === optionCount) && 'opacity-50 cursor-not-allowed'
              )}
            >
              {t('products.attributes.selectAll')}
            </button>
            <span className="text-muted-foreground">|</span>
            <button
              type="button"
              onClick={handleClearAll}
              disabled={disabled || selectedValues.length === 0}
              className={cn(
                'text-muted-foreground hover:text-foreground transition-colors cursor-pointer',
                (disabled || selectedValues.length === 0) && 'opacity-50 cursor-not-allowed'
              )}
            >
              {t('products.attributes.clearAll')}
            </button>
          </div>
        </div>
      )}

      {/* Options grid - responsive columns */}
      <div
        className={cn(
          'grid gap-2',
          // Responsive columns based on option count
          optionCount <= 2 ? 'grid-cols-1 sm:grid-cols-2' :
          optionCount <= 4 ? 'grid-cols-1 sm:grid-cols-2' :
          optionCount <= 6 ? 'grid-cols-1 sm:grid-cols-2 md:grid-cols-3' :
          'grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4'
        )}
        role="group"
        aria-label={field.name}
      >
        {field.options?.map((option) => {
          const isSelected = selectedValues.includes(option.value)
          return (
            <label
              key={option.id}
              className={cn(
                'flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-all duration-200',
                'hover:bg-muted/50 hover:border-muted-foreground/30',
                isSelected && 'bg-primary/5 border-primary/40 shadow-sm',
                disabled && 'opacity-50 cursor-not-allowed hover:bg-transparent hover:border-border'
              )}
            >
              <Checkbox
                checked={isSelected}
                onCheckedChange={(checked) => handleToggle(option.value, !!checked)}
                disabled={disabled}
                aria-label={option.displayValue}
                className="cursor-pointer"
              />
              <div className="flex-1 min-w-0">
                <span className={cn(
                  'text-sm block truncate',
                  isSelected && 'font-medium'
                )}>
                  {option.displayValue}
                </span>
                {option.colorCode && (
                  <div className="flex items-center gap-1.5 mt-0.5">
                    <div
                      className="w-3 h-3 rounded-full border border-gray-200"
                      style={{ backgroundColor: option.colorCode }}
                    />
                    <span className="text-xs text-muted-foreground font-mono">
                      {option.colorCode}
                    </span>
                  </div>
                )}
              </div>
              {isSelected && (
                <Check className="h-4 w-4 text-primary shrink-0" />
              )}
            </label>
          )
        })}
      </div>
    </div>
  )
}

/** Text input */
const TextInput = ({ field, value, onChange, disabled }: InputComponentProps) => {
  return (
    <Input
      id={`attr-${field.attributeId}`}
      value={(value as string) || ''}
      onChange={(e) => onChange(e.target.value || null)}
      placeholder={field.placeholder || ''}
      maxLength={field.maxLength || undefined}
      disabled={disabled}
      aria-label={field.name}
      aria-required={field.isRequired}
    />
  )
}

/** Textarea input */
const TextAreaInput = ({ field, value, onChange, disabled }: InputComponentProps) => {
  return (
    <Textarea
      id={`attr-${field.attributeId}`}
      value={(value as string) || ''}
      onChange={(e) => onChange(e.target.value || null)}
      placeholder={field.placeholder || ''}
      maxLength={field.maxLength || undefined}
      disabled={disabled}
      rows={3}
      aria-label={field.name}
      aria-required={field.isRequired}
    />
  )
}

/** Number input with increment/decrement buttons */
const NumberInput = ({
  field,
  value,
  onChange,
  disabled,
  isDecimal,
}: InputComponentProps & { isDecimal: boolean }) => {
  const { t } = useTranslation('common')
  const numValue = value !== null && value !== undefined ? Number(value) : null
  const step = isDecimal ? 0.1 : 1

  const handleIncrement = () => {
    const current = numValue ?? (field.minValue ?? 0)
    const newValue = current + step
    if (field.maxValue === null || field.maxValue === undefined || newValue <= field.maxValue) {
      onChange(isDecimal ? Number(newValue.toFixed(2)) : newValue)
    }
  }

  const handleDecrement = () => {
    const current = numValue ?? (field.minValue ?? 0)
    const newValue = current - step
    if (field.minValue === null || field.minValue === undefined || newValue >= field.minValue) {
      onChange(isDecimal ? Number(newValue.toFixed(2)) : newValue)
    }
  }

  const isAtMin = field.minValue !== null && field.minValue !== undefined && numValue !== null && numValue <= field.minValue
  const isAtMax = field.maxValue !== null && field.maxValue !== undefined && numValue !== null && numValue >= field.maxValue

  // Clamp value within bounds on blur
  const handleBlur = () => {
    if (numValue === null) return
    let clamped = numValue
    if (field.minValue !== null && field.minValue !== undefined && numValue < field.minValue) {
      clamped = field.minValue
    }
    if (field.maxValue !== null && field.maxValue !== undefined && numValue > field.maxValue) {
      clamped = field.maxValue
    }
    if (clamped !== numValue) {
      onChange(isDecimal ? Number(clamped.toFixed(2)) : clamped)
    }
  }

  return (
    <div className="flex items-stretch">
      {/* Decrement button */}
      <Button
        type="button"
        variant="outline"
        size="icon"
        onClick={handleDecrement}
        disabled={disabled || isAtMin}
        className={cn(
          'rounded-r-none border-r-0 h-10 w-10 shrink-0 cursor-pointer',
          'hover:bg-muted transition-colors'
        )}
        aria-label={t('products.attributes.decrement')}
      >
        <Minus className="h-4 w-4" />
      </Button>

      {/* Number input */}
      <Input
        id={`attr-${field.attributeId}`}
        type="number"
        value={numValue !== null ? String(numValue) : ''}
        onChange={(e) => {
          const val = e.target.value
          if (val === '') {
            onChange(null)
          } else {
            onChange(isDecimal ? parseFloat(val) : parseInt(val, 10))
          }
        }}
        onBlur={handleBlur}
        placeholder={field.placeholder || '0'}
        min={field.minValue ?? undefined}
        max={field.maxValue ?? undefined}
        step={isDecimal ? 'any' : 1}
        disabled={disabled}
        className={cn(
          'rounded-none text-center flex-1',
          '[appearance:textfield]',
          '[&::-webkit-outer-spin-button]:appearance-none',
          '[&::-webkit-inner-spin-button]:appearance-none'
        )}
        aria-label={field.name}
        aria-required={field.isRequired}
      />

      {/* Increment button */}
      <Button
        type="button"
        variant="outline"
        size="icon"
        onClick={handleIncrement}
        disabled={disabled || isAtMax}
        className={cn(
          'rounded-l-none border-l-0 h-10 w-10 shrink-0 cursor-pointer',
          'hover:bg-muted transition-colors'
        )}
        aria-label={t('products.attributes.increment')}
      >
        <Plus className="h-4 w-4" />
      </Button>
    </div>
  )
}

/** Boolean switch toggle */
const BooleanInput = ({ field, value, onChange, disabled }: InputComponentProps) => {
  const { t } = useTranslation('common')

  return (
    <div className="flex items-center gap-3">
      <Switch
        id={`attr-${field.attributeId}`}
        checked={!!value}
        onCheckedChange={(checked) => onChange(checked)}
        disabled={disabled}
        aria-label={field.name}
        aria-required={field.isRequired}
        className="cursor-pointer"
      />
      <span className="text-sm text-muted-foreground">
        {value ? t('labels.yes') : t('labels.no')}
      </span>
    </div>
  )
}

/** Date/DateTime picker with enhanced time selection */
const DateInput = ({
  field,
  value,
  onChange,
  disabled,
  showTime,
}: InputComponentProps & { showTime: boolean }) => {
  const { t } = useTranslation('common')
  const dateValue = value ? parseISO(value as string) : undefined
  const formatStr = showTime ? 'PPP HH:mm' : 'PPP'

  // Common time presets for quick selection
  const timePresets = [
    { label: '9:00 AM', hour: 9, minute: 0 },
    { label: '12:00 PM', hour: 12, minute: 0 },
    { label: '3:00 PM', hour: 15, minute: 0 },
    { label: '6:00 PM', hour: 18, minute: 0 },
  ]

  const handleTimeChange = (hour: number, minute: number) => {
    const newDate = dateValue ? new Date(dateValue) : new Date()
    newDate.setHours(hour, minute, 0, 0)
    onChange(newDate.toISOString())
  }

  const handleClear = () => {
    onChange(null)
  }

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          id={`attr-${field.attributeId}`}
          variant="outline"
          className={cn(
            'w-full justify-start text-left font-normal cursor-pointer group',
            !dateValue && 'text-muted-foreground'
          )}
          disabled={disabled}
          aria-label={field.name}
          aria-required={field.isRequired}
        >
          <CalendarIcon className="mr-2 h-4 w-4 shrink-0" />
          <span className="flex-1 truncate">
            {dateValue ? format(dateValue, formatStr) : field.placeholder || t('products.attributes.pickDate')}
          </span>
          {showTime && dateValue && (
            <Clock className="ml-2 h-4 w-4 text-muted-foreground shrink-0" />
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start">
        <div className="p-3 border-b flex items-center justify-between gap-2">
          <span className="text-sm font-medium">
            {showTime ? t('products.attributes.selectDateTime') : t('products.attributes.selectDate')}
          </span>
          {dateValue && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleClear}
              className="h-7 px-2 text-xs cursor-pointer"
            >
              {t('labels.clear')}
            </Button>
          )}
        </div>
        <Calendar
          mode="single"
          selected={dateValue}
          onSelect={(date) => {
            if (date) {
              // Preserve time if already set
              if (dateValue && showTime) {
                date.setHours(dateValue.getHours(), dateValue.getMinutes())
              }
              onChange(date.toISOString())
            } else {
              onChange(null)
            }
          }}
          initialFocus
        />
        {showTime && (
          <div className="p-3 border-t space-y-3">
            {/* Time input */}
            <div className="flex items-center gap-2">
              <Clock className="h-4 w-4 text-muted-foreground shrink-0" />
              <Input
                type="time"
                value={dateValue ? format(dateValue, 'HH:mm') : ''}
                onChange={(e) => {
                  const [hours, minutes] = e.target.value.split(':').map(Number)
                  handleTimeChange(hours, minutes)
                }}
                className="flex-1"
                aria-label={t('products.attributes.time')}
              />
            </div>

            {/* Quick time presets */}
            <div className="grid grid-cols-4 gap-1">
              {timePresets.map((preset) => (
                <Button
                  key={preset.label}
                  variant="outline"
                  size="sm"
                  className={cn(
                    'text-xs h-7 cursor-pointer',
                    dateValue &&
                    dateValue.getHours() === preset.hour &&
                    dateValue.getMinutes() === preset.minute &&
                    'bg-primary/10 border-primary/50'
                  )}
                  onClick={() => handleTimeChange(preset.hour, preset.minute)}
                >
                  {preset.label}
                </Button>
              ))}
            </div>
          </div>
        )}
      </PopoverContent>
    </Popover>
  )
}

/** Color picker with swatches + native picker + hex input */
const ColorInput = ({ field, value, onChange, disabled }: InputComponentProps) => {
  const { t } = useTranslation('common')
  const selectedValue = (value as string) || ''
  const hasOptions = field.options && field.options.length > 0

  // Validate hex color format
  const isValidHex = (hex: string) => /^#[0-9A-Fa-f]{6}$/.test(hex)
  const normalizeHex = (hex: string) => {
    if (!hex) return ''
    const clean = hex.replace(/^#/, '')
    if (clean.length === 3) {
      // Expand shorthand (#RGB -> #RRGGBB)
      return `#${clean[0]}${clean[0]}${clean[1]}${clean[1]}${clean[2]}${clean[2]}`
    }
    return hex.startsWith('#') ? hex : `#${hex}`
  }

  return (
    <div className="space-y-3">
      {/* Native color picker + hex input row */}
      <div className="flex items-center gap-3">
        {/* Native color picker */}
        <div className="relative">
          <input
            type="color"
            value={isValidHex(selectedValue) ? selectedValue : '#000000'}
            onChange={(e) => onChange(e.target.value)}
            disabled={disabled}
            className={cn(
              'w-12 h-10 rounded-lg cursor-pointer border-2 border-input',
              'hover:border-primary/50 transition-colors',
              '[&::-webkit-color-swatch-wrapper]:p-1',
              '[&::-webkit-color-swatch]:rounded-md',
              '[&::-moz-color-swatch]:rounded-md',
              disabled && 'opacity-50 cursor-not-allowed'
            )}
            aria-label={t('products.attributes.pickColor')}
          />
        </div>

        {/* Hex input */}
        <div className="flex-1 relative">
          <Input
            value={selectedValue}
            onChange={(e) => {
              const hex = normalizeHex(e.target.value)
              onChange(hex || null)
            }}
            placeholder="#RRGGBB"
            maxLength={7}
            disabled={disabled}
            className={cn(
              'font-mono uppercase',
              selectedValue && !isValidHex(selectedValue) && 'border-destructive'
            )}
            aria-label={t('products.attributes.hexColor')}
          />
          {selectedValue && isValidHex(selectedValue) && (
            <div
              className="absolute right-3 top-1/2 -translate-y-1/2 w-5 h-5 rounded border border-input"
              style={{ backgroundColor: selectedValue }}
              aria-hidden="true"
            />
          )}
        </div>
      </div>

      {/* Predefined color swatches */}
      {hasOptions && (
        <div className="space-y-2">
          <span className="text-xs text-muted-foreground">
            {t('products.attributes.presetColors')}
          </span>
          <div
            className="flex flex-wrap gap-2"
            role="radiogroup"
            aria-label={field.name}
          >
            {field.options?.map((option, index) => {
              const colorValue = option.colorCode || option.value
              const isSelected = selectedValue === colorValue
              return (
                <TooltipProvider key={option.id}>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <button
                        type="button"
                        onClick={() => onChange(colorValue)}
                        disabled={disabled}
                        className={cn(
                          'w-9 h-9 rounded-lg cursor-pointer transition-all duration-200',
                          'focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary',
                          'border-2 shadow-sm hover:shadow-md hover:scale-110',
                          isSelected
                            ? 'ring-2 ring-offset-2 ring-primary border-primary scale-110'
                            : 'border-gray-200 dark:border-gray-700',
                          disabled && 'opacity-50 cursor-not-allowed hover:scale-100'
                        )}
                        style={{ backgroundColor: colorValue }}
                        aria-label={isSelected
                          ? t('products.attributes.colorSelected', { color: option.displayValue, defaultValue: `${option.displayValue} (selected)` })
                          : option.displayValue}
                        aria-checked={isSelected}
                        role="radio"
                        tabIndex={index === 0 || isSelected ? 0 : -1}
                      >
                        {isSelected && (
                          <Check
                            className={cn(
                              'h-4 w-4 mx-auto drop-shadow-md',
                              isLightColor(colorValue) ? 'text-gray-800' : 'text-white'
                            )}
                          />
                        )}
                      </button>
                    </TooltipTrigger>
                    <TooltipContent side="top" className="text-xs">
                      <p>{option.displayValue}</p>
                      <p className="text-muted-foreground font-mono">{colorValue}</p>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}

/** Range input with visual slider and dual inputs */
const RangeInput = ({ field, value, onChange, disabled }: InputComponentProps) => {
  const { t } = useTranslation('common')
  // Value stored as "min-max" string
  const [minVal, maxVal] = ((value as string) || '').split('-').map(v => v === '' ? NaN : Number(v))

  // Get bounds from field config or use defaults
  const fieldMin = field.minValue ?? 0
  const fieldMax = field.maxValue ?? 100
  const rangeSize = fieldMax - fieldMin

  // Calculate visual positions for the range bar
  const minPosition = !isNaN(minVal) ? ((minVal - fieldMin) / rangeSize) * 100 : 0
  const maxPosition = !isNaN(maxVal) ? ((maxVal - fieldMin) / rangeSize) * 100 : 100

  const handleMinChange = (newMin: string) => {
    const minValue = newMin === '' ? '' : Number(newMin)
    const maxValue = isNaN(maxVal) ? '' : maxVal
    onChange(`${minValue}-${maxValue}`)
  }

  const handleMaxChange = (newMax: string) => {
    const minValue = isNaN(minVal) ? '' : minVal
    const maxValue = newMax === '' ? '' : Number(newMax)
    onChange(`${minValue}-${maxValue}`)
  }

  // Handle slider drag for min thumb
  const handleMinSlider = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newMin = Number(e.target.value)
    // Ensure min doesn't exceed max
    const effectiveMax = isNaN(maxVal) ? fieldMax : maxVal
    if (newMin <= effectiveMax) {
      handleMinChange(String(newMin))
    }
  }

  // Handle slider drag for max thumb
  const handleMaxSlider = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newMax = Number(e.target.value)
    // Ensure max doesn't go below min
    const effectiveMin = isNaN(minVal) ? fieldMin : minVal
    if (newMax >= effectiveMin) {
      handleMaxChange(String(newMax))
    }
  }

  return (
    <div className="space-y-4">
      {/* Visual slider track */}
      <div className="relative pt-2 pb-6">
        {/* Track background */}
        <div className="h-2 bg-muted rounded-full relative">
          {/* Active range highlight */}
          <div
            className="absolute h-full bg-primary/60 rounded-full transition-all duration-150"
            style={{
              left: `${minPosition}%`,
              width: `${Math.max(0, maxPosition - minPosition)}%`,
            }}
          />
        </div>

        {/* Min thumb (range input) */}
        <input
          type="range"
          min={fieldMin}
          max={fieldMax}
          value={isNaN(minVal) ? fieldMin : minVal}
          onChange={handleMinSlider}
          disabled={disabled}
          className={cn(
            'absolute top-0 w-full h-2 appearance-none bg-transparent pointer-events-none',
            '[&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:pointer-events-auto',
            '[&::-webkit-slider-thumb]:w-5 [&::-webkit-slider-thumb]:h-5 [&::-webkit-slider-thumb]:rounded-full',
            '[&::-webkit-slider-thumb]:bg-primary [&::-webkit-slider-thumb]:border-2 [&::-webkit-slider-thumb]:border-background',
            '[&::-webkit-slider-thumb]:shadow-md [&::-webkit-slider-thumb]:cursor-pointer',
            '[&::-webkit-slider-thumb]:hover:bg-primary/90 [&::-webkit-slider-thumb]:transition-colors',
            '[&::-moz-range-thumb]:w-5 [&::-moz-range-thumb]:h-5 [&::-moz-range-thumb]:rounded-full',
            '[&::-moz-range-thumb]:bg-primary [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-background',
            '[&::-moz-range-thumb]:shadow-md [&::-moz-range-thumb]:cursor-pointer',
            '[&::-moz-range-thumb]:pointer-events-auto',
            disabled && 'opacity-50 cursor-not-allowed'
          )}
          aria-label={t('products.attributes.fieldMin', { field: field.name, defaultValue: `${field.name} ${t('products.attributes.min')}` })}
        />

        {/* Max thumb (range input) */}
        <input
          type="range"
          min={fieldMin}
          max={fieldMax}
          value={isNaN(maxVal) ? fieldMax : maxVal}
          onChange={handleMaxSlider}
          disabled={disabled}
          className={cn(
            'absolute top-0 w-full h-2 appearance-none bg-transparent pointer-events-none',
            '[&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:pointer-events-auto',
            '[&::-webkit-slider-thumb]:w-5 [&::-webkit-slider-thumb]:h-5 [&::-webkit-slider-thumb]:rounded-full',
            '[&::-webkit-slider-thumb]:bg-primary [&::-webkit-slider-thumb]:border-2 [&::-webkit-slider-thumb]:border-background',
            '[&::-webkit-slider-thumb]:shadow-md [&::-webkit-slider-thumb]:cursor-pointer',
            '[&::-webkit-slider-thumb]:hover:bg-primary/90 [&::-webkit-slider-thumb]:transition-colors',
            '[&::-moz-range-thumb]:w-5 [&::-moz-range-thumb]:h-5 [&::-moz-range-thumb]:rounded-full',
            '[&::-moz-range-thumb]:bg-primary [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-background',
            '[&::-moz-range-thumb]:shadow-md [&::-moz-range-thumb]:cursor-pointer',
            '[&::-moz-range-thumb]:pointer-events-auto',
            disabled && 'opacity-50 cursor-not-allowed'
          )}
          aria-label={t('products.attributes.fieldMax', { field: field.name, defaultValue: `${field.name} ${t('products.attributes.max')}` })}
        />

        {/* Bounds labels */}
        <div className="absolute -bottom-1 left-0 right-0 flex justify-between text-xs text-muted-foreground">
          <span>{fieldMin}{field.unit && ` ${field.unit}`}</span>
          <span>{fieldMax}{field.unit && ` ${field.unit}`}</span>
        </div>
      </div>

      {/* Numeric inputs for precise entry */}
      <div className="flex items-center gap-3">
        <div className="flex-1">
          <label className="text-xs text-muted-foreground mb-1 block">
            {t('products.attributes.min')}
          </label>
          <Input
            type="number"
            value={isNaN(minVal) ? '' : minVal}
            onChange={(e) => handleMinChange(e.target.value)}
            placeholder={String(fieldMin)}
            min={fieldMin}
            max={fieldMax}
            disabled={disabled}
            className="text-center"
            aria-label={t('products.attributes.fieldMin', { field: field.name, defaultValue: `${field.name} ${t('products.attributes.min')}` })}
          />
        </div>
        <div className="flex items-center justify-center pt-5">
          <div className="w-4 h-0.5 bg-muted-foreground/50 rounded" />
        </div>
        <div className="flex-1">
          <label className="text-xs text-muted-foreground mb-1 block">
            {t('products.attributes.max')}
          </label>
          <Input
            type="number"
            value={isNaN(maxVal) ? '' : maxVal}
            onChange={(e) => handleMaxChange(e.target.value)}
            placeholder={String(fieldMax)}
            min={fieldMin}
            max={fieldMax}
            disabled={disabled}
            className="text-center"
            aria-label={t('products.attributes.fieldMax', { field: field.name, defaultValue: `${field.name} ${t('products.attributes.max')}` })}
          />
        </div>
      </div>

      {/* Current selection display */}
      {(!isNaN(minVal) || !isNaN(maxVal)) && (
        <div className="text-sm text-center text-muted-foreground bg-muted/50 rounded-md py-2 px-3">
          {t('products.attributes.rangeSelected', {
            min: isNaN(minVal) ? fieldMin : minVal,
            max: isNaN(maxVal) ? fieldMax : maxVal,
            unit: field.unit || '',
          })}
        </div>
      )}
    </div>
  )
}

/** URL input with validation indicator */
const UrlInput = ({ field, value, onChange, disabled }: InputComponentProps) => {
  const urlValue = (value as string) || ''
  const isValidUrl = urlValue === '' || isValidUrlString(urlValue)

  return (
    <div className="relative">
      <Input
        id={`attr-${field.attributeId}`}
        type="url"
        value={urlValue}
        onChange={(e) => onChange(e.target.value || null)}
        placeholder={field.placeholder || 'https://'}
        disabled={disabled}
        className="pr-10"
        aria-label={field.name}
        aria-required={field.isRequired}
        aria-invalid={!isValidUrl}
      />
      <div className="absolute right-3 top-1/2 -translate-y-1/2">
        {urlValue && (
          isValidUrl ? (
            <LinkIcon className="h-4 w-4 text-green-500" />
          ) : (
            <AlertCircle className="h-4 w-4 text-destructive" />
          )
        )}
      </div>
    </div>
  )
}

/** File input with drag-and-drop support */
const FileInput = ({ field, value, onChange, disabled }: InputComponentProps) => {
  const { t } = useTranslation('common')
  const [preview, setPreview] = useState<string | null>(null)
  const [fileName, setFileName] = useState<string | null>(null)

  const onDrop = useCallback((acceptedFiles: File[]) => {
    const file = acceptedFiles[0]
    if (file) {
      setFileName(file.name)
      // For images, create preview
      if (file.type.startsWith('image/')) {
        const reader = new FileReader()
        reader.onload = () => {
          setPreview(reader.result as string)
        }
        reader.readAsDataURL(file)
      } else {
        setPreview(null)
      }
      // Store file reference or base64 - in production this would upload to server
      // For now, store filename as value
      onChange(file.name)
    }
  }, [onChange])

  // Cast to access optional properties that may be defined for File type attributes
  const fieldWithFileProps = field as typeof field & {
    acceptedFileTypes?: string[]
    maxFileSize?: number
  }

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    disabled,
    multiple: false,
    accept: fieldWithFileProps.acceptedFileTypes
      ? fieldWithFileProps.acceptedFileTypes.reduce<Record<string, string[]>>(
          (acc, type) => ({ ...acc, [type]: [] }),
          {}
        )
      : undefined,
    maxSize: fieldWithFileProps.maxFileSize || 10 * 1024 * 1024, // 10MB default
  })

  const handleClear = (e: React.MouseEvent) => {
    e.stopPropagation()
    onChange(null)
    setPreview(null)
    setFileName(null)
  }

  const hasValue = value || fileName

  return (
    <div
      {...getRootProps()}
      className={cn(
        'relative border-2 border-dashed rounded-lg transition-all duration-200 cursor-pointer',
        'hover:border-primary/50 hover:bg-muted/30',
        isDragActive && 'border-primary bg-primary/5 scale-[1.01]',
        hasValue ? 'p-3' : 'p-6',
        disabled && 'opacity-50 cursor-not-allowed hover:border-border hover:bg-transparent'
      )}
    >
      <input {...getInputProps()} />

      {hasValue ? (
        <div className="flex items-center gap-3">
          {/* Preview or icon */}
          <div className="shrink-0">
            {preview ? (
              <div className="w-14 h-14 rounded-lg overflow-hidden border bg-muted">
                <img
                  src={preview}
                  alt={fileName || ''}
                  className="w-full h-full object-cover"
                />
              </div>
            ) : (
              <div className="w-14 h-14 rounded-lg border bg-muted flex items-center justify-center">
                <FileIcon className="h-6 w-6 text-muted-foreground" />
              </div>
            )}
          </div>

          {/* File info */}
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium truncate">
              {fileName || String(value)}
            </p>
            <p className="text-xs text-muted-foreground">
              {t('products.attributes.clickToReplace')}
            </p>
          </div>

          {/* Clear button */}
          {!disabled && (
            <button
              type="button"
              onClick={handleClear}
              className={cn(
                'shrink-0 p-2 rounded-full cursor-pointer',
                'hover:bg-destructive/10 text-muted-foreground hover:text-destructive',
                'transition-colors'
              )}
              aria-label={t('products.attributes.removeFile')}
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
      ) : (
        <div className="text-center space-y-3">
          <div className={cn(
            'mx-auto w-12 h-12 rounded-full flex items-center justify-center',
            'bg-muted transition-colors',
            isDragActive && 'bg-primary/10'
          )}>
            {isDragActive ? (
              <Image className="h-6 w-6 text-primary animate-bounce" />
            ) : (
              <Upload className="h-6 w-6 text-muted-foreground" />
            )}
          </div>
          <div>
            <p className="text-sm font-medium">
              {isDragActive
                ? t('products.attributes.dropFileHere')
                : t('products.attributes.dragDropOrClick')
              }
            </p>
            <p className="text-xs text-muted-foreground mt-1">
              {fieldWithFileProps.maxFileSize
                ? t('products.attributes.maxFileSize', { size: formatFileSize(fieldWithFileProps.maxFileSize) })
                : t('products.attributes.maxFileSize', { size: '10 MB' })
              }
            </p>
          </div>
        </div>
      )}
    </div>
  )
}

// ============================================================================
// Utility Functions
// ============================================================================

const isLightColor = (color: string): boolean => {
  // Simple heuristic for light colors
  if (!color) return false
  const hex = color.replace('#', '')
  if (hex.length !== 6) return false
  const r = parseInt(hex.slice(0, 2), 16)
  const g = parseInt(hex.slice(2, 4), 16)
  const b = parseInt(hex.slice(4, 6), 16)
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255
  return luminance > 0.5
}

const isValidUrlString = (url: string): boolean => {
  try {
    new URL(url)
    return true
  } catch {
    return false
  }
}

const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 Bytes'
  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}
