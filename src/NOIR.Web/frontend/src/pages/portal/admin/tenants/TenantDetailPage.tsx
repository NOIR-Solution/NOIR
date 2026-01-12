import { useState, useEffect } from 'react'
import { useParams, useNavigate, useSearchParams, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { ArrowLeft, Edit, Trash2 } from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { TenantForm } from './components/TenantForm'
import { getTenant, updateTenant, deleteTenant } from '@/services/tenants'
import { ApiError } from '@/services/apiClient'
import type { Tenant, UpdateTenantRequest } from '@/types'

export default function TenantDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const { t } = useTranslation('common')

  const [tenant, setTenant] = useState<Tenant | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [editDialogOpen, setEditDialogOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return

    const fetchTenant = async () => {
      setLoading(true)
      setError(null)
      try {
        const data = await getTenant(id)
        setTenant(data)

        // Auto-open edit dialog if ?edit=true
        if (searchParams.get('edit') === 'true') {
          setEditDialogOpen(true)
          // Remove the query param from URL
          setSearchParams({}, { replace: true })
        }
      } catch (err) {
        const message = err instanceof ApiError
          ? err.message
          : 'Failed to load tenant'
        setError(message)
      } finally {
        setLoading(false)
      }
    }

    fetchTenant()
  }, [id, searchParams, setSearchParams])

  const handleUpdate = async (data: UpdateTenantRequest) => {
    if (!id) return
    setSaving(true)
    try {
      const updated = await updateTenant(id, data)
      setTenant(updated)
      toast.success(t('messages.updateSuccess'))
      setEditDialogOpen(false)
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : t('messages.operationFailed')
      toast.error(message)
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!id) return
    setSaving(true)
    try {
      await deleteTenant(id)
      toast.success(t('messages.deleteSuccess'))
      navigate('/portal/admin/tenants')
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : t('messages.operationFailed')
      toast.error(message)
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-8">
        <p className="text-muted-foreground">{t('labels.loading')}</p>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-4">
        <Button variant="ghost" asChild>
          <Link to="/portal/admin/tenants">
            <ArrowLeft className="mr-2 h-4 w-4" />
            {t('buttons.back')}
          </Link>
        </Button>
        <div className="p-4 bg-destructive/10 text-destructive rounded-md">
          {error}
        </div>
      </div>
    )
  }

  if (!tenant) {
    return null
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Button variant="ghost" asChild>
            <Link to="/portal/admin/tenants">
              <ArrowLeft className="mr-2 h-4 w-4" />
              {t('buttons.back')}
            </Link>
          </Button>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">{tenant.name || tenant.identifier}</h1>
            <p className="text-muted-foreground font-mono">{tenant.identifier}</p>
          </div>
        </div>
        <div className="flex items-center space-x-2">
          <Button variant="outline" onClick={() => setEditDialogOpen(true)}>
            <Edit className="mr-2 h-4 w-4" />
            {t('buttons.edit')}
          </Button>
          <Button variant="destructive" onClick={() => setDeleteDialogOpen(true)}>
            <Trash2 className="mr-2 h-4 w-4" />
            {t('buttons.delete')}
          </Button>
        </div>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('tenants.details.basicInfo')}</CardTitle>
            <CardDescription>{t('tenants.details.basicInfoDescription')}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-sm font-medium text-muted-foreground">{t('tenants.table.identifier')}</p>
              <p className="font-mono">{tenant.identifier}</p>
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">{t('tenants.table.name')}</p>
              <p>{tenant.name || '-'}</p>
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">{t('labels.status')}</p>
              <Badge variant={tenant.isActive ? 'default' : 'secondary'}>
                {tenant.isActive ? t('labels.active') : t('labels.inactive')}
              </Badge>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('tenants.details.timestamps')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-sm font-medium text-muted-foreground">{t('labels.createdAt')}</p>
              <p>{new Date(tenant.createdAt).toLocaleString()}</p>
            </div>
            {tenant.modifiedAt && (
              <div>
                <p className="text-sm font-medium text-muted-foreground">{t('labels.updatedAt')}</p>
                <p>{new Date(tenant.modifiedAt).toLocaleString()}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Edit Dialog */}
      <Dialog open={editDialogOpen} onOpenChange={setEditDialogOpen}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>{t('tenants.editTitle')}</DialogTitle>
            <DialogDescription>{t('tenants.editDescription')}</DialogDescription>
          </DialogHeader>
          <TenantForm
            tenant={tenant}
            onSubmit={handleUpdate}
            onCancel={() => setEditDialogOpen(false)}
            loading={saving}
          />
        </DialogContent>
      </Dialog>

      {/* Delete Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('tenants.deleteTitle')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('tenants.deleteDescription', { name: tenant.name || tenant.identifier })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={saving}>{t('buttons.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              disabled={saving}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {saving ? t('labels.loading') : t('buttons.delete')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
