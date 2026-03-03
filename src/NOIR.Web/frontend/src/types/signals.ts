export interface EntityUpdateSignal {
  entityType: string
  entityId: string
  operation: 'Created' | 'Updated' | 'Deleted'
  updatedAt: string
}
