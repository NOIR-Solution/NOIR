import type { TFunction } from 'i18next'

interface TypeBadgeConfig {
  label: string
  className: string
}

const TYPE_STYLES: Record<string, string> = {
  Select: 'bg-blue-100 text-blue-700 border-blue-200 dark:bg-blue-950 dark:text-blue-300 dark:border-blue-800',
  MultiSelect: 'bg-indigo-100 text-indigo-700 border-indigo-200 dark:bg-indigo-950 dark:text-indigo-300 dark:border-indigo-800',
  Text: 'bg-slate-100 text-slate-700 border-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700',
  TextArea: 'bg-stone-100 text-stone-700 border-stone-200 dark:bg-stone-800 dark:text-stone-300 dark:border-stone-700',
  Number: 'bg-emerald-100 text-emerald-700 border-emerald-200 dark:bg-emerald-950 dark:text-emerald-300 dark:border-emerald-800',
  Decimal: 'bg-teal-100 text-teal-700 border-teal-200 dark:bg-teal-950 dark:text-teal-300 dark:border-teal-800',
  Boolean: 'bg-amber-100 text-amber-700 border-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
  Date: 'bg-cyan-100 text-cyan-700 border-cyan-200 dark:bg-cyan-950 dark:text-cyan-300 dark:border-cyan-800',
  DateTime: 'bg-sky-100 text-sky-700 border-sky-200 dark:bg-sky-950 dark:text-sky-300 dark:border-sky-800',
  Color: 'bg-pink-100 text-pink-700 border-pink-200 dark:bg-pink-950 dark:text-pink-300 dark:border-pink-800',
  Range: 'bg-orange-100 text-orange-700 border-orange-200 dark:bg-orange-950 dark:text-orange-300 dark:border-orange-800',
  Url: 'bg-violet-100 text-violet-700 border-violet-200 dark:bg-violet-950 dark:text-violet-300 dark:border-violet-800',
  File: 'bg-rose-100 text-rose-700 border-rose-200 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-800',
}

const TYPE_LABEL_KEYS: Record<string, { key: string; fallback: string }> = {
  Select: { key: 'productAttributes.types.select', fallback: 'Select' },
  MultiSelect: { key: 'productAttributes.types.multiSelect', fallback: 'Multi-Select' },
  Text: { key: 'productAttributes.types.text', fallback: 'Text' },
  TextArea: { key: 'productAttributes.types.textArea', fallback: 'Text Area' },
  Number: { key: 'productAttributes.types.number', fallback: 'Number' },
  Decimal: { key: 'productAttributes.types.decimal', fallback: 'Decimal' },
  Boolean: { key: 'productAttributes.types.boolean', fallback: 'Boolean' },
  Date: { key: 'productAttributes.types.date', fallback: 'Date' },
  DateTime: { key: 'productAttributes.types.dateTime', fallback: 'Date Time' },
  Color: { key: 'productAttributes.types.color', fallback: 'Color' },
  Range: { key: 'productAttributes.types.range', fallback: 'Range' },
  Url: { key: 'productAttributes.types.url', fallback: 'URL' },
  File: { key: 'productAttributes.types.file', fallback: 'File' },
}

export const getTypeBadge = (type: string, t: TFunction): TypeBadgeConfig => {
  const labelConfig = TYPE_LABEL_KEYS[type]
  const label = labelConfig ? t(labelConfig.key, labelConfig.fallback) : type
  const className = TYPE_STYLES[type] || ''
  return { label, className }
}
