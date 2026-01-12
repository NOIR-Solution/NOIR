/**
 * RetentionPolicyManager - Manage audit retention policies
 *
 * Features:
 * - List all retention policies
 * - Create new policies with compliance presets
 * - Edit existing policies
 * - Delete policies
 * - Visual storage tier indicators
 */
import { useState, useEffect } from 'react'
import {
  Plus,
  Edit,
  Trash2,
  Save,
  X,
  Shield,
  Clock,
  Archive,
  AlertTriangle,
  FileArchive,
} from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import {
  getRetentionPolicies,
  getCompliancePresets,
  createRetentionPolicy,
  updateRetentionPolicy,
  deleteRetentionPolicy,
} from '@/services/audit'
import type {
  AuditRetentionPolicy,
  CompliancePreset,
  CreateRetentionPolicyRequest,
  UpdateRetentionPolicyRequest,
} from '@/types'

function getComplianceBadgeColor(preset: string | null): string {
  switch (preset?.toUpperCase()) {
    case 'GDPR': return 'bg-blue-500/10 text-blue-500 border-blue-500/20'
    case 'SOX': return 'bg-purple-500/10 text-purple-500 border-purple-500/20'
    case 'HIPAA': return 'bg-green-500/10 text-green-500 border-green-500/20'
    case 'PCI-DSS':
    case 'PCI': return 'bg-orange-500/10 text-orange-500 border-orange-500/20'
    case 'CUSTOM': return 'bg-gray-500/10 text-gray-500 border-gray-500/20'
    default: return 'bg-gray-500/10 text-gray-500 border-gray-500/20'
  }
}

function formatDays(days: number): string {
  if (days >= 365) {
    const years = Math.floor(days / 365)
    const remainingDays = days % 365
    if (remainingDays === 0) return `${years}y`
    return `${years}y ${remainingDays}d`
  }
  return `${days}d`
}

interface PolicyFormData {
  name: string
  description: string
  hotStorageDays: number
  warmStorageDays: number
  coldStorageDays: number
  deleteAfterDays: number
  entityTypes: string
  compliancePreset: string
  exportBeforeArchive: boolean
  exportBeforeDelete: boolean
  isActive: boolean
  priority: number
}

const defaultFormData: PolicyFormData = {
  name: '',
  description: '',
  hotStorageDays: 30,
  warmStorageDays: 90,
  coldStorageDays: 365,
  deleteAfterDays: 2555,
  entityTypes: '',
  compliancePreset: 'CUSTOM',
  exportBeforeArchive: true,
  exportBeforeDelete: true,
  isActive: true,
  priority: 0,
}

function StorageTierVisual({ policy }: { policy: AuditRetentionPolicy }) {
  const total = policy.deleteAfterDays
  const hotWidth = (policy.hotStorageDays / total) * 100
  const warmWidth = ((policy.warmStorageDays - policy.hotStorageDays) / total) * 100
  const coldWidth = ((policy.coldStorageDays - policy.warmStorageDays) / total) * 100
  const deleteWidth = ((policy.deleteAfterDays - policy.coldStorageDays) / total) * 100

  return (
    <div className="space-y-2">
      <div className="flex h-4 rounded-full overflow-hidden">
        <div
          className="bg-green-500 flex items-center justify-center text-[10px] text-white"
          style={{ width: `${hotWidth}%` }}
          title={`Hot: ${policy.hotStorageDays} days`}
        />
        <div
          className="bg-yellow-500 flex items-center justify-center text-[10px] text-white"
          style={{ width: `${warmWidth}%` }}
          title={`Warm: ${policy.warmStorageDays} days`}
        />
        <div
          className="bg-blue-500 flex items-center justify-center text-[10px] text-white"
          style={{ width: `${coldWidth}%` }}
          title={`Cold: ${policy.coldStorageDays} days`}
        />
        <div
          className="bg-red-500 flex items-center justify-center text-[10px] text-white"
          style={{ width: `${deleteWidth}%` }}
          title={`Delete: ${policy.deleteAfterDays} days`}
        />
      </div>
      <div className="flex justify-between text-xs text-muted-foreground">
        <span className="flex items-center gap-1">
          <div className="w-2 h-2 rounded-full bg-green-500" />
          Hot: {formatDays(policy.hotStorageDays)}
        </span>
        <span className="flex items-center gap-1">
          <div className="w-2 h-2 rounded-full bg-yellow-500" />
          Warm: {formatDays(policy.warmStorageDays)}
        </span>
        <span className="flex items-center gap-1">
          <div className="w-2 h-2 rounded-full bg-blue-500" />
          Cold: {formatDays(policy.coldStorageDays)}
        </span>
        <span className="flex items-center gap-1">
          <div className="w-2 h-2 rounded-full bg-red-500" />
          Delete: {formatDays(policy.deleteAfterDays)}
        </span>
      </div>
    </div>
  )
}

function PolicyCard({
  policy,
  onEdit,
  onDelete,
}: {
  policy: AuditRetentionPolicy
  onEdit: () => void
  onDelete: () => void
}) {
  return (
    <Card className={!policy.isActive ? 'opacity-60' : ''}>
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between">
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <CardTitle className="text-lg">{policy.name}</CardTitle>
              {!policy.isActive && (
                <Badge variant="outline" className="text-xs">Inactive</Badge>
              )}
            </div>
            <CardDescription>{policy.description || 'No description'}</CardDescription>
          </div>
          <div className="flex items-center gap-2">
            {policy.compliancePreset && (
              <Badge variant="outline" className={getComplianceBadgeColor(policy.compliancePreset)}>
                <Shield className="h-3 w-3 mr-1" />
                {policy.compliancePreset}
              </Badge>
            )}
            <Button variant="ghost" size="icon" onClick={onEdit}>
              <Edit className="h-4 w-4" />
            </Button>
            <Button variant="ghost" size="icon" onClick={onDelete}>
              <Trash2 className="h-4 w-4 text-destructive" />
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <StorageTierVisual policy={policy} />

        <div className="flex flex-wrap gap-2">
          {policy.exportBeforeArchive && (
            <Badge variant="secondary" className="text-xs">
              <FileArchive className="h-3 w-3 mr-1" />
              Export before archive
            </Badge>
          )}
          {policy.exportBeforeDelete && (
            <Badge variant="secondary" className="text-xs">
              <Archive className="h-3 w-3 mr-1" />
              Export before delete
            </Badge>
          )}
          <Badge variant="secondary" className="text-xs">
            Priority: {policy.priority}
          </Badge>
        </div>

        {policy.entityTypes && policy.entityTypes.length > 0 && (
          <div>
            <span className="text-xs text-muted-foreground">Applies to: </span>
            <span className="text-xs">{policy.entityTypes.join(', ')}</span>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

function PolicyForm({
  initialData,
  presets,
  onSave,
  onCancel,
  isNew,
}: {
  initialData: PolicyFormData
  presets: CompliancePreset[]
  onSave: (data: PolicyFormData) => void
  onCancel: () => void
  isNew: boolean
}) {
  const [formData, setFormData] = useState<PolicyFormData>(initialData)
  const [errors, setErrors] = useState<Record<string, string>>({})

  const handlePresetChange = (presetCode: string) => {
    const preset = presets.find(p => p.code === presetCode)
    if (preset) {
      setFormData(prev => ({
        ...prev,
        compliancePreset: presetCode,
        hotStorageDays: preset.hotStorageDays,
        warmStorageDays: preset.warmStorageDays,
        coldStorageDays: preset.coldStorageDays,
        deleteAfterDays: preset.deleteAfterDays,
      }))
    }
  }

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.name.trim()) {
      newErrors.name = 'Name is required'
    }
    if (formData.hotStorageDays < 0) {
      newErrors.hotStorageDays = 'Must be non-negative'
    }
    if (formData.warmStorageDays < formData.hotStorageDays) {
      newErrors.warmStorageDays = 'Must be >= hot storage days'
    }
    if (formData.coldStorageDays < formData.warmStorageDays) {
      newErrors.coldStorageDays = 'Must be >= warm storage days'
    }
    if (formData.deleteAfterDays < formData.coldStorageDays) {
      newErrors.deleteAfterDays = 'Must be >= cold storage days'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = () => {
    if (validate()) {
      onSave(formData)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{isNew ? 'Create Retention Policy' : 'Edit Retention Policy'}</CardTitle>
        <CardDescription>Configure audit log retention settings</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Basic Info */}
        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="name">Name *</Label>
            <Input
              id="name"
              value={formData.name}
              onChange={e => setFormData(prev => ({ ...prev, name: e.target.value }))}
              placeholder="Policy name"
            />
            {errors.name && <p className="text-xs text-destructive">{errors.name}</p>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Input
              id="description"
              value={formData.description}
              onChange={e => setFormData(prev => ({ ...prev, description: e.target.value }))}
              placeholder="Optional description"
            />
          </div>
        </div>

        {/* Compliance Preset */}
        <div className="space-y-2">
          <Label>Compliance Preset</Label>
          <div className="flex flex-wrap gap-2">
            {presets.map(preset => (
              <Button
                key={preset.code}
                type="button"
                variant={formData.compliancePreset === preset.code ? 'default' : 'outline'}
                size="sm"
                onClick={() => handlePresetChange(preset.code)}
              >
                <Shield className="h-3 w-3 mr-1" />
                {preset.code}
              </Button>
            ))}
          </div>
          {formData.compliancePreset && (
            <p className="text-xs text-muted-foreground">
              {presets.find(p => p.code === formData.compliancePreset)?.description}
            </p>
          )}
        </div>

        {/* Retention Periods */}
        <div className="grid gap-4 md:grid-cols-4">
          <div className="space-y-2">
            <Label htmlFor="hot" className="flex items-center gap-1">
              <div className="w-2 h-2 rounded-full bg-green-500" />
              Hot Storage (days)
            </Label>
            <Input
              id="hot"
              type="number"
              min="0"
              value={formData.hotStorageDays}
              onChange={e => setFormData(prev => ({ ...prev, hotStorageDays: parseInt(e.target.value) || 0 }))}
            />
            {errors.hotStorageDays && <p className="text-xs text-destructive">{errors.hotStorageDays}</p>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="warm" className="flex items-center gap-1">
              <div className="w-2 h-2 rounded-full bg-yellow-500" />
              Warm Storage (days)
            </Label>
            <Input
              id="warm"
              type="number"
              min="0"
              value={formData.warmStorageDays}
              onChange={e => setFormData(prev => ({ ...prev, warmStorageDays: parseInt(e.target.value) || 0 }))}
            />
            {errors.warmStorageDays && <p className="text-xs text-destructive">{errors.warmStorageDays}</p>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="cold" className="flex items-center gap-1">
              <div className="w-2 h-2 rounded-full bg-blue-500" />
              Cold Storage (days)
            </Label>
            <Input
              id="cold"
              type="number"
              min="0"
              value={formData.coldStorageDays}
              onChange={e => setFormData(prev => ({ ...prev, coldStorageDays: parseInt(e.target.value) || 0 }))}
            />
            {errors.coldStorageDays && <p className="text-xs text-destructive">{errors.coldStorageDays}</p>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="delete" className="flex items-center gap-1">
              <div className="w-2 h-2 rounded-full bg-red-500" />
              Delete After (days)
            </Label>
            <Input
              id="delete"
              type="number"
              min="0"
              value={formData.deleteAfterDays}
              onChange={e => setFormData(prev => ({ ...prev, deleteAfterDays: parseInt(e.target.value) || 0 }))}
            />
            {errors.deleteAfterDays && <p className="text-xs text-destructive">{errors.deleteAfterDays}</p>}
          </div>
        </div>

        {/* Options */}
        <div className="grid gap-4 md:grid-cols-3">
          <div className="space-y-2">
            <Label htmlFor="entityTypes">Entity Types (comma-separated)</Label>
            <Input
              id="entityTypes"
              value={formData.entityTypes}
              onChange={e => setFormData(prev => ({ ...prev, entityTypes: e.target.value }))}
              placeholder="Leave empty for all"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="priority">Priority</Label>
            <Input
              id="priority"
              type="number"
              value={formData.priority}
              onChange={e => setFormData(prev => ({ ...prev, priority: parseInt(e.target.value) || 0 }))}
            />
          </div>
          <div className="flex items-end gap-4">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={formData.exportBeforeArchive}
                onChange={e => setFormData(prev => ({ ...prev, exportBeforeArchive: e.target.checked }))}
                className="h-4 w-4"
              />
              <span className="text-sm">Export before archive</span>
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={formData.exportBeforeDelete}
                onChange={e => setFormData(prev => ({ ...prev, exportBeforeDelete: e.target.checked }))}
                className="h-4 w-4"
              />
              <span className="text-sm">Export before delete</span>
            </label>
          </div>
        </div>

        {!isNew && (
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={formData.isActive}
              onChange={e => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
              className="h-4 w-4"
            />
            <span className="text-sm">Policy is active</span>
          </label>
        )}

        {/* Actions */}
        <div className="flex justify-end gap-2 pt-4">
          <Button variant="outline" onClick={onCancel}>
            <X className="h-4 w-4 mr-2" />
            Cancel
          </Button>
          <Button onClick={handleSubmit}>
            <Save className="h-4 w-4 mr-2" />
            {isNew ? 'Create Policy' : 'Save Changes'}
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}

export function RetentionPolicyManager() {
  const [policies, setPolicies] = useState<AuditRetentionPolicy[]>([])
  const [presets, setPresets] = useState<CompliancePreset[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [editingPolicy, setEditingPolicy] = useState<AuditRetentionPolicy | null>(null)
  const [isCreating, setIsCreating] = useState(false)

  // Load policies and presets
  useEffect(() => {
    const loadData = async () => {
      setIsLoading(true)
      try {
        const [policiesData, presetsData] = await Promise.all([
          getRetentionPolicies(),
          getCompliancePresets(),
        ])
        setPolicies(policiesData)
        setPresets(presetsData)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load data')
      } finally {
        setIsLoading(false)
      }
    }
    loadData()
  }, [])

  const handleCreate = async (formData: PolicyFormData) => {
    try {
      const request: CreateRetentionPolicyRequest = {
        name: formData.name,
        description: formData.description || undefined,
        hotStorageDays: formData.hotStorageDays,
        warmStorageDays: formData.warmStorageDays,
        coldStorageDays: formData.coldStorageDays,
        deleteAfterDays: formData.deleteAfterDays,
        entityTypes: formData.entityTypes ? formData.entityTypes.split(',').map(s => s.trim()).filter(Boolean) : undefined,
        compliancePreset: formData.compliancePreset || undefined,
        exportBeforeArchive: formData.exportBeforeArchive,
        exportBeforeDelete: formData.exportBeforeDelete,
        priority: formData.priority,
      }
      const newPolicy = await createRetentionPolicy(request)
      setPolicies(prev => [...prev, newPolicy])
      setIsCreating(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create policy')
    }
  }

  const handleUpdate = async (formData: PolicyFormData) => {
    if (!editingPolicy) return

    try {
      const request: UpdateRetentionPolicyRequest = {
        name: formData.name,
        description: formData.description || undefined,
        hotStorageDays: formData.hotStorageDays,
        warmStorageDays: formData.warmStorageDays,
        coldStorageDays: formData.coldStorageDays,
        deleteAfterDays: formData.deleteAfterDays,
        entityTypes: formData.entityTypes ? formData.entityTypes.split(',').map(s => s.trim()).filter(Boolean) : undefined,
        compliancePreset: formData.compliancePreset || undefined,
        exportBeforeArchive: formData.exportBeforeArchive,
        exportBeforeDelete: formData.exportBeforeDelete,
        isActive: formData.isActive,
        priority: formData.priority,
      }
      const updatedPolicy = await updateRetentionPolicy(editingPolicy.id, request)
      setPolicies(prev => prev.map(p => p.id === editingPolicy.id ? updatedPolicy : p))
      setEditingPolicy(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update policy')
    }
  }

  const handleDelete = async (policy: AuditRetentionPolicy) => {
    if (!confirm(`Are you sure you want to delete "${policy.name}"?`)) return

    try {
      await deleteRetentionPolicy(policy.id)
      setPolicies(prev => prev.filter(p => p.id !== policy.id))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete policy')
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    )
  }

  if (isCreating) {
    return (
      <PolicyForm
        initialData={defaultFormData}
        presets={presets}
        onSave={handleCreate}
        onCancel={() => setIsCreating(false)}
        isNew
      />
    )
  }

  if (editingPolicy) {
    return (
      <PolicyForm
        initialData={{
          name: editingPolicy.name,
          description: editingPolicy.description || '',
          hotStorageDays: editingPolicy.hotStorageDays,
          warmStorageDays: editingPolicy.warmStorageDays,
          coldStorageDays: editingPolicy.coldStorageDays,
          deleteAfterDays: editingPolicy.deleteAfterDays,
          entityTypes: editingPolicy.entityTypes?.join(', ') || '',
          compliancePreset: editingPolicy.compliancePreset || 'CUSTOM',
          exportBeforeArchive: editingPolicy.exportBeforeArchive,
          exportBeforeDelete: editingPolicy.exportBeforeDelete,
          isActive: editingPolicy.isActive,
          priority: editingPolicy.priority,
        }}
        presets={presets}
        onSave={handleUpdate}
        onCancel={() => setEditingPolicy(null)}
        isNew={false}
      />
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Retention Policies</h1>
          <p className="text-muted-foreground">Configure audit log retention and compliance settings</p>
        </div>
        <Button onClick={() => setIsCreating(true)}>
          <Plus className="h-4 w-4 mr-2" />
          New Policy
        </Button>
      </div>

      {/* Error */}
      {error && (
        <div className="bg-destructive/10 border border-destructive/20 rounded-lg p-4 flex items-center gap-2">
          <AlertTriangle className="h-5 w-5 text-destructive" />
          <span className="text-destructive">{error}</span>
          <Button variant="ghost" size="sm" onClick={() => setError(null)} className="ml-auto">
            <X className="h-4 w-4" />
          </Button>
        </div>
      )}

      {/* Policies Grid */}
      {policies.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center h-64 text-center">
            <Clock className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium">No retention policies</h3>
            <p className="text-muted-foreground mb-4">
              Create a retention policy to manage how long audit logs are stored
            </p>
            <Button onClick={() => setIsCreating(true)}>
              <Plus className="h-4 w-4 mr-2" />
              Create First Policy
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {policies.map(policy => (
            <PolicyCard
              key={policy.id}
              policy={policy}
              onEdit={() => setEditingPolicy(policy)}
              onDelete={() => handleDelete(policy)}
            />
          ))}
        </div>
      )}

      {/* Compliance Info */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" />
            Compliance Presets
          </CardTitle>
          <CardDescription>Pre-configured retention periods for common regulations</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            {presets.map(preset => (
              <div key={preset.code} className="p-3 border rounded-lg">
                <div className="flex items-center gap-2 mb-2">
                  <Badge variant="outline" className={getComplianceBadgeColor(preset.code)}>
                    {preset.code}
                  </Badge>
                </div>
                <p className="text-sm font-medium">{preset.name}</p>
                <p className="text-xs text-muted-foreground">{preset.description}</p>
                <div className="mt-2 text-xs space-y-1">
                  <div className="flex justify-between">
                    <span>Hot:</span>
                    <span>{formatDays(preset.hotStorageDays)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span>Delete:</span>
                    <span>{formatDays(preset.deleteAfterDays)}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

export default RetentionPolicyManager
