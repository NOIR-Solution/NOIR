import type { TFunction } from 'i18next'

/**
 * Translates English audit action descriptions to the current locale.
 *
 * Backend stores audit descriptions as hardcoded English strings (e.g. "Created brand 'Nike'").
 * This utility pattern-matches common formats and translates the verb + entity type
 * while preserving entity names.
 *
 * Falls back to the raw description if no pattern matches.
 */

type PatternTranslator = {
  pattern: RegExp
  translate: (t: TFunction, match: RegExpMatchArray) => string
}

const ENTITY_TYPE_KEYS: Record<string, string> = {
  'brand': 'audit.entities.brand',
  'blog post': 'audit.entities.blogPost',
  'blog tag': 'audit.entities.blogTag',
  'blog category': 'audit.entities.blogCategory',
  'product': 'audit.entities.product',
  'product category': 'audit.entities.productCategory',
  'product attribute': 'audit.entities.productAttribute',
  'order': 'audit.entities.order',
  'customer': 'audit.entities.customer',
  'customer group': 'audit.entities.customerGroup',
  'customer address': 'audit.entities.customerAddress',
  'employee': 'audit.entities.employee',
  'department': 'audit.entities.department',
  'employee tag': 'audit.entities.employeeTag',
  'project': 'audit.entities.project',
  'project column': 'audit.entities.projectColumn',
  'task': 'audit.entities.task',
  'label': 'audit.entities.label',
  'role': 'audit.entities.role',
  'user': 'audit.entities.user',
  'tenant': 'audit.entities.tenant',
  'promotion': 'audit.entities.promotion',
  'review': 'audit.entities.review',
  'wishlist': 'audit.entities.wishlist',
  'email template': 'audit.entities.emailTemplate',
  'API key': 'audit.entities.apiKey',
  'lead': 'audit.entities.lead',
  'contact': 'audit.entities.contact',
  'company': 'audit.entities.company',
  'activity': 'audit.entities.activity',
  'pipeline': 'audit.entities.pipeline',
  'inventory receipt': 'audit.entities.inventoryReceipt',
  'payment': 'audit.entities.payment',
  'shipping provider': 'audit.entities.shippingProvider',
  'webhook': 'audit.entities.webhook',
  'media': 'audit.entities.media',
}

const translateEntity = (t: TFunction, entity: string): string => {
  const key = ENTITY_TYPE_KEYS[entity.toLowerCase()]
  return key ? t(key, entity) : entity
}

const PATTERNS: PatternTranslator[] = [
  // "Created {type} '{name}'"
  {
    pattern: /^Created (.+?) '(.+)'$/,
    translate: (t, m) => t('audit.actions.createdEntity', { entity: translateEntity(t, m[1]), name: m[2] }),
  },
  // "Updated {type} '{name}'"
  {
    pattern: /^Updated (.+?) '(.+)'$/,
    translate: (t, m) => t('audit.actions.updatedEntity', { entity: translateEntity(t, m[1]), name: m[2] }),
  },
  // "Deleted {type} '{name}'"
  {
    pattern: /^Deleted (.+?) '(.+)'$/,
    translate: (t, m) => t('audit.actions.deletedEntity', { entity: translateEntity(t, m[1]), name: m[2] }),
  },
  // "Bulk {action} {count} {items}"
  {
    pattern: /^Bulk (\w+) (\d+) (.+)$/,
    translate: (t, m) => {
      const verb = t(`audit.verbs.${m[1].toLowerCase()}`, m[1])
      return t('audit.actions.bulk', `Bulk ${m[1]} ${m[2]} ${m[3]}`, { action: verb, count: m[2], items: m[3] } as Record<string, string>)
    },
  },
  // Status changes: "Changed {entity} status to {status}"
  {
    pattern: /^Changed (.+?) status to ['\s]*(.+?)['\s]*$/,
    translate: (t, m) => t('audit.actions.changedStatus', { entity: translateEntity(t, m[1]), status: m[2] }),
  },
  // "Changed task status to '{status}'"
  {
    pattern: /^Changed task status to '(.+)'$/,
    translate: (t, m) => t('audit.actions.changedTaskStatus', { status: m[1] }),
  },
  // Task/Kanban specific
  { pattern: /^Moved task on Kanban board$/, translate: (t) => t('audit.actions.movedTaskKanban') },
  { pattern: /^Archived task$/, translate: (t) => t('audit.actions.archivedTask') },
  { pattern: /^Restored task from archive$/, translate: (t) => t('audit.actions.restoredTask') },
  { pattern: /^Moved task to column '(.+)'$/, translate: (t, m) => t('audit.actions.movedTaskToColumn', { column: m[1] }) },
  { pattern: /^Added label to task$/, translate: (t) => t('audit.actions.addedLabelToTask') },
  { pattern: /^Removed label from task$/, translate: (t) => t('audit.actions.removedLabelFromTask') },
  { pattern: /^Deleted project column$/, translate: (t) => t('audit.actions.deletedProjectColumn') },
  // CRM specific
  { pattern: /^Moved lead to a different stage$/, translate: (t) => t('audit.actions.movedLeadStage') },
  { pattern: /^Marked lead as Won$/, translate: (t) => t('audit.actions.markedLeadWon') },
  { pattern: /^Marked lead as Lost$/, translate: (t) => t('audit.actions.markedLeadLost') },
  // Auth specific
  { pattern: /^Changed password$/, translate: (t) => t('audit.actions.changedPassword') },
  { pattern: /^Updated profile$/, translate: (t) => t('audit.actions.updatedProfile') },
  { pattern: /^Uploaded avatar$/, translate: (t) => t('audit.actions.uploadedAvatar') },
  { pattern: /^Deleted avatar$/, translate: (t) => t('audit.actions.deletedAvatar') },
  { pattern: /^Revoked session$/, translate: (t) => t('audit.actions.revokedSession') },
  // Inventory
  {
    pattern: /^Created (\w+) inventory receipt$/,
    translate: (t, m) => t('audit.actions.createdInventoryReceipt', { type: m[1] }),
  },
]

/**
 * Translate an English audit description to the current locale.
 * Returns the original description if no pattern matches.
 */
export const translateAuditDescription = (t: TFunction, description: string): string => {
  if (!description) return description

  for (const { pattern, translate } of PATTERNS) {
    const match = description.match(pattern)
    if (match) {
      return translate(t, match)
    }
  }

  return description
}
