import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Pencil,
  Plus,
  Tags,
  Trash2,
  Users,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useUrlEditDialog } from '@/hooks/useUrlEditDialog'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  EmptyState,
  PageHeader,
  Skeleton,
} from '@uikit'
import { useTagsQuery, useDeleteTag } from '@/portal-app/hr/queries'
import { TagFormDialog } from '../../components/TagFormDialog'
import type { EmployeeTagDto, EmployeeTagCategory } from '@/types/hr'

const CATEGORY_ORDER: EmployeeTagCategory[] = ['Team', 'Skill', 'Project', 'Location', 'Seniority', 'Employment', 'Custom']

export const TagsPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Tags')

  const { hasPermission } = usePermissions()
  const canManage = hasPermission(Permissions.HrTagsManage)
  const { data: tags, isLoading } = useTagsQuery()
  const deleteMutation = useDeleteTag()

  const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-employee-tag' })

  const allTags = tags ?? []
  const { editItem: tagToEdit, openEdit: openEditTag, closeEdit: closeEditTag } = useUrlEditDialog<EmployeeTagDto>(allTags)

  const [tagToDelete, setTagToDelete] = useState<EmployeeTagDto | null>(null)

  // Group tags by category
  const tagsByCategory = useMemo(() =>
    CATEGORY_ORDER
      .map(cat => ({
        category: cat,
        tags: allTags.filter(tag => tag.category === cat),
      }))
      .filter(group => group.tags.length > 0),
    [allTags],
  )

  const handleDelete = async () => {
    if (!tagToDelete) return
    try {
      await deleteMutation.mutateAsync(tagToDelete.id)
      toast.success(t('hr.tags.tagDeleted'))
      setTagToDelete(null)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Tags}
        title={t('hr.tags.title')}
        description={t('hr.tags.description')}
        responsive
        action={
          canManage ? (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => openCreate()}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('hr.tags.createTag')}
            </Button>
          ) : undefined
        }
      />

      {isLoading ? (
        <div className="space-y-6">
          {[...Array(3)].map((_, i) => (
            <Card key={i} className="shadow-sm">
              <CardHeader className="pb-3">
                <Skeleton className="h-5 w-32" />
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-3">
                  {[...Array(4)].map((_, j) => (
                    <Skeleton key={j} className="h-16 w-48 rounded-lg" />
                  ))}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : allTags.length === 0 ? (
        <EmptyState
          icon={Tags}
          title={t('hr.tags.noTags')}
          description={t('hr.tags.noTagsDescription')}
          action={canManage ? {
            label: t('hr.tags.createTag'),
            onClick: () => openCreate(),
          } : undefined}
        />
      ) : (
        <div className="space-y-6">
          {tagsByCategory.map(({ category, tags: categoryTags }) => (
            <Card key={category} className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">{t(`hr.tags.categories.${category}`)}</CardTitle>
                <CardDescription>
                  {t('hr.tags.employeeCount', { count: categoryTags.length })}
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                  {categoryTags.map((tag) => (
                    <div
                      key={tag.id}
                      className="flex items-center justify-between gap-3 rounded-lg border p-3 transition-all hover:bg-muted/50"
                    >
                      <div className="flex items-center gap-3 min-w-0">
                        <span
                          className="h-3 w-3 rounded-full flex-shrink-0"
                          style={{ backgroundColor: tag.color }}
                        />
                        <div className="min-w-0">
                          <p className="text-sm font-medium truncate">{tag.name}</p>
                          {tag.description && (
                            <p className="text-xs text-muted-foreground truncate">{tag.description}</p>
                          )}
                        </div>
                      </div>
                      <div className="flex items-center gap-2 flex-shrink-0">
                        <Badge variant="outline" className="text-xs">
                          <Users className="h-3 w-3 mr-1" />
                          {tag.employeeCount}
                        </Badge>
                        {canManage && (
                          <div className="flex items-center gap-1">
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-7 w-7 p-0 cursor-pointer"
                              onClick={() => openEditTag(tag)}
                              aria-label={t('hr.tags.editTag')}
                            >
                              <Pencil className="h-3.5 w-3.5" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-7 w-7 p-0 cursor-pointer text-destructive hover:text-destructive"
                              onClick={() => setTagToDelete(tag)}
                              aria-label={`${t('hr.tags.deleteTag')} ${tag.name}`}
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </Button>
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Create/Edit Tag Dialog */}
      <TagFormDialog
        open={isCreateOpen || !!tagToEdit}
        onOpenChange={(open) => {
          if (!open) {
            if (isCreateOpen) onCreateOpenChange(false)
            if (tagToEdit) closeEditTag()
          }
        }}
        tag={tagToEdit}
      />

      {/* Delete Confirmation Dialog */}
      <Credenza open={!!tagToDelete} onOpenChange={(open) => !open && setTagToDelete(null)}>
        <CredenzaContent className="border-destructive/30">
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <CredenzaTitle>{t('hr.tags.deleteTag')}</CredenzaTitle>
                <CredenzaDescription>
                  {t('hr.tags.deleteConfirmation')}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button
              variant="outline"
              onClick={() => setTagToDelete(null)}
              disabled={deleteMutation.isPending}
              className="cursor-pointer"
            >
              {t('labels.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {t('hr.tags.deleteTag')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

export default TagsPage
