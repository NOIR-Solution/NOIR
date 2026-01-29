/**
 * AttributeInputFactory - Routes to correct input component by AttributeType
 * Phase 9: Product Form Attribute Integration
 */
import { useTranslation } from 'react-i18next'
import { HelpCircle } from 'lucide-react'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Switch } from '@/components/ui/switch'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Button } from '@/components/ui/button'
import { CalendarIcon, Check, Link as LinkIcon, AlertCircle } from 'lucide-react'
import { format, parseISO } from 'date-fns'
import { cn } from '@/lib/utils'
import type { AttributeInputProps, AttributeValue } from './types'
import type { AttributeType } from '@/types/productAttribute'

/**
 * Factory component that renders the appropriate input based on attribute type
 */
export function AttributeInputFactory({
  field,
  value,
  onChange,
  disabled = false,
  error,
}: AttributeInputProps) {
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
function SelectInput({ field, value, onChange, disabled }: InputComponentProps) {
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

/** Multi-select checkbox group */
function MultiSelectInput({ field, value, onChange, disabled }: InputComponentProps) {
  const selectedValues = Array.isArray(value) ? value : []

  const handleToggle = (optionValue: string, checked: boolean) => {
    if (checked) {
      onChange([...selectedValues, optionValue])
    } else {
      onChange(selectedValues.filter((v) => v !== optionValue))
    }
  }

  return (
    <div
      className="grid grid-cols-2 gap-2"
      role="group"
      aria-label={field.name}
    >
      {field.options?.map((option) => (
        <label
          key={option.id}
          className={cn(
            'flex items-center gap-2 p-2 rounded-md border cursor-pointer transition-colors',
            'hover:bg-muted/50',
            selectedValues.includes(option.value) && 'bg-primary/5 border-primary/30',
            disabled && 'opacity-50 cursor-not-allowed'
          )}
        >
          <Checkbox
            checked={selectedValues.includes(option.value)}
            onCheckedChange={(checked) => handleToggle(option.value, !!checked)}
            disabled={disabled}
            aria-label={option.displayValue}
            className="cursor-pointer"
          />
          <span className="text-sm">{option.displayValue}</span>
        </label>
      ))}
    </div>
  )
}

/** Text input */
function TextInput({ field, value, onChange, disabled }: InputComponentProps) {
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
function TextAreaInput({ field, value, onChange, disabled }: InputComponentProps) {
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

/** Number input (integer or decimal) */
function NumberInput({
  field,
  value,
  onChange,
  disabled,
  isDecimal,
}: InputComponentProps & { isDecimal: boolean }) {
  return (
    <Input
      id={`attr-${field.attributeId}`}
      type="number"
      value={value !== null && value !== undefined ? String(value) : ''}
      onChange={(e) => {
        const val = e.target.value
        if (val === '') {
          onChange(null)
        } else {
          onChange(isDecimal ? parseFloat(val) : parseInt(val, 10))
        }
      }}
      placeholder={field.placeholder || ''}
      min={field.minValue ?? undefined}
      max={field.maxValue ?? undefined}
      step={isDecimal ? 'any' : 1}
      disabled={disabled}
      aria-label={field.name}
      aria-required={field.isRequired}
    />
  )
}

/** Boolean switch toggle */
function BooleanInput({ field, value, onChange, disabled }: InputComponentProps) {
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

/** Date/DateTime picker */
function DateInput({
  field,
  value,
  onChange,
  disabled,
  showTime,
}: InputComponentProps & { showTime: boolean }) {
  const { t } = useTranslation('common')
  const dateValue = value ? parseISO(value as string) : undefined
  const formatStr = showTime ? 'PPP HH:mm' : 'PPP'

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          id={`attr-${field.attributeId}`}
          variant="outline"
          className={cn(
            'w-full justify-start text-left font-normal cursor-pointer',
            !dateValue && 'text-muted-foreground'
          )}
          disabled={disabled}
          aria-label={field.name}
          aria-required={field.isRequired}
        >
          <CalendarIcon className="mr-2 h-4 w-4" />
          {dateValue ? format(dateValue, formatStr) : field.placeholder || t('products.attributes.pickDate')}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start">
        <Calendar
          mode="single"
          selected={dateValue}
          onSelect={(date) => {
            if (date) {
              onChange(date.toISOString())
            } else {
              onChange(null)
            }
          }}
          initialFocus
        />
        {showTime && dateValue && (
          <div className="p-3 border-t">
            <Input
              type="time"
              value={format(dateValue, 'HH:mm')}
              onChange={(e) => {
                const [hours, minutes] = e.target.value.split(':').map(Number)
                const newDate = new Date(dateValue)
                newDate.setHours(hours, minutes)
                onChange(newDate.toISOString())
              }}
              aria-label={t('products.attributes.time')}
            />
          </div>
        )}
      </PopoverContent>
    </Popover>
  )
}

/** Color swatch picker */
function ColorInput({ field, value, onChange, disabled }: InputComponentProps) {
  const selectedValue = value as string

  return (
    <div
      className="flex flex-wrap gap-2"
      role="radiogroup"
      aria-label={field.name}
    >
      {field.options?.map((option, index) => {
        const isSelected = selectedValue === option.value
        return (
          <button
            key={option.id}
            type="button"
            onClick={() => onChange(option.value)}
            disabled={disabled}
            className={cn(
              'w-8 h-8 rounded-full cursor-pointer transition-all duration-200',
              'focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary',
              'border-2',
              isSelected ? 'ring-2 ring-offset-2 ring-primary border-primary' : 'border-gray-200',
              disabled && 'opacity-50 cursor-not-allowed'
            )}
            style={{
              backgroundColor: option.colorCode || option.value,
            }}
            aria-label={`${option.displayValue}${isSelected ? ' (selected)' : ''}`}
            aria-checked={isSelected}
            role="radio"
            tabIndex={index === 0 || isSelected ? 0 : -1}
          >
            {isSelected && (
              <Check
                className={cn(
                  'h-4 w-4 mx-auto',
                  // Use contrasting color for checkmark
                  isLightColor(option.colorCode || option.value) ? 'text-gray-800' : 'text-white'
                )}
              />
            )}
          </button>
        )
      })}
    </div>
  )
}

/** Range input (min/max) */
function RangeInput({ field, value, onChange, disabled }: InputComponentProps) {
  const { t } = useTranslation('common')
  // Value stored as "min-max" string
  const [min, max] = ((value as string) || '').split('-').map(Number)

  const handleMinChange = (newMin: string) => {
    const minVal = newMin === '' ? '' : Number(newMin)
    const maxVal = max || ''
    onChange(`${minVal}-${maxVal}`)
  }

  const handleMaxChange = (newMax: string) => {
    const minVal = min || ''
    const maxVal = newMax === '' ? '' : Number(newMax)
    onChange(`${minVal}-${maxVal}`)
  }

  return (
    <div className="flex items-center gap-2">
      <Input
        type="number"
        value={min || ''}
        onChange={(e) => handleMinChange(e.target.value)}
        placeholder={t('products.attributes.min')}
        disabled={disabled}
        className="flex-1"
        aria-label={`${field.name} ${t('products.attributes.min')}`}
      />
      <span className="text-muted-foreground">-</span>
      <Input
        type="number"
        value={max || ''}
        onChange={(e) => handleMaxChange(e.target.value)}
        placeholder={t('products.attributes.max')}
        disabled={disabled}
        className="flex-1"
        aria-label={`${field.name} ${t('products.attributes.max')}`}
      />
    </div>
  )
}

/** URL input with validation indicator */
function UrlInput({ field, value, onChange, disabled }: InputComponentProps) {
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

/** File input placeholder */
function FileInput({ field, value, onChange, disabled }: InputComponentProps) {
  const { t } = useTranslation('common')

  return (
    <div className="border-2 border-dashed rounded-lg p-4 text-center text-muted-foreground">
      <p className="text-sm">
        {t('products.attributes.fileUploadPlaceholder')}
      </p>
      {value && (
        <p className="text-xs mt-2 text-primary">
          {t('products.attributes.currentFile')}: {String(value)}
        </p>
      )}
    </div>
  )
}

// ============================================================================
// Utility Functions
// ============================================================================

function isLightColor(color: string): boolean {
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

function isValidUrlString(url: string): boolean {
  try {
    new URL(url)
    return true
  } catch {
    return false
  }
}
