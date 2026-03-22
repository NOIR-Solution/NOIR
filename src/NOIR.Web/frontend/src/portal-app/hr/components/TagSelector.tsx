import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Check, Tags } from 'lucide-react'
import { toast } from 'sonner'
import {
  Button,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  EmptyState,
} from '@uikit'
import { useTagsQuery, useAssignTags, useRemoveTags } from '@/portal-app/hr/queries'
import type { TagBriefDto, EmployeeTagCategory } from '@/types/hr'

const CATEGORY_ORDER: EmployeeTagCategory[] = ['Team', 'Skill', 'Project', 'Location', 'Seniority', 'Employment', 'Custom']

interface TagSelectorProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  employeeId: string
  currentTags: TagBriefDto[]
}

export const TagSelector = ({ open, onOpenChange, employeeId, currentTags }: TagSelectorProps) => {
  const { t } = useTranslation('common')
  const { data: allTags } = useTagsQuery()
  const assignMutation = useAssignTags()
  const removeMutation = useRemoveTags()

  const [selectedIds, setSelectedIds] = useState<Set<string>>(() => new Set(currentTags.map(tag => tag.id)))

  // Reset selected when dialog opens
  const handleOpenChange = (isOpen: boolean) => {
    if (isOpen) {
      setSelectedIds(new Set(currentTags.map(tag => tag.id)))
    }
    onOpenChange(isOpen)
  }

  const toggleTag = (tagId: string) => {
    setSelectedIds(prev => {
      const next = new Set(prev)
      if (next.has(tagId)) {
        next.delete(tagId)
      } else {
        next.add(tagId)
      }
      return next
    })
  }

  const handleSave = async () => {
    const currentIds = new Set(currentTags.map(tag => tag.id))
    const toAssign = [...selectedIds].filter(id => !currentIds.has(id))
    const toRemove = [...currentIds].filter(id => !selectedIds.has(id))

    try {
      if (toAssign.length > 0) {
        await assignMutation.mutateAsync({ employeeId, data: { tagIds: toAssign } })
      }
      if (toRemove.length > 0) {
        await removeMutation.mutateAsync({ employeeId, data: { tagIds: toRemove } })
      }
      toast.success(t('hr.tags.tagAssigned'))
      onOpenChange(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('errors.generic', 'An error occurred')
      toast.error(message)
    }
  }

  const isPending = assignMutation.isPending || removeMutation.isPending

  // Group tags by category
  const tagsByCategory = CATEGORY_ORDER
    .map(cat => ({
      category: cat,
      tags: (allTags ?? []).filter(tag => tag.category === cat),
    }))
    .filter(group => group.tags.length > 0)

  return (
    <Credenza open={open} onOpenChange={handleOpenChange}>
      <CredenzaContent className="sm:max-w-[480px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
              <Tags className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>{t('hr.tags.manageTags')}</CredenzaTitle>
              <CredenzaDescription>{t('hr.tags.selectTags')}</CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>
        <CredenzaBody>
          <div className="space-y-4">
            {tagsByCategory.map(({ category, tags }) => (
              <div key={category}>
                <p className="text-xs font-medium text-muted-foreground mb-2">
                  {t(`hr.tags.categories.${category}`)}
                </p>
                <div className="flex flex-wrap gap-2">
                  {tags.map((tag) => {
                    const isSelected = selectedIds.has(tag.id)
                    return (
                      <button
                        key={tag.id}
                        type="button"
                        className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border cursor-pointer transition-all ${
                          isSelected
                            ? 'ring-2 ring-offset-1'
                            : 'opacity-60 hover:opacity-100'
                        }`}
                        style={{
                          borderColor: tag.color,
                          color: tag.color,
                          ...(isSelected ? { ringColor: tag.color } : {}),
                        }}
                        onClick={() => toggleTag(tag.id)}
                      >
                        <span
                          className="inline-block h-2 w-2 rounded-full flex-shrink-0"
                          style={{ backgroundColor: tag.color }}
                        />
                        {tag.name}
                        {isSelected && <Check className="h-3 w-3" />}
                      </button>
                    )
                  })}
                </div>
              </div>
            ))}
            {tagsByCategory.length === 0 && (
              <EmptyState
                icon={Tags}
                title={t('hr.tags.noTags')}
                description={t('hr.tags.noTagsDescription')}
              />
            )}
          </div>
        </CredenzaBody>
        <CredenzaFooter>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isPending}
            className="cursor-pointer"
          >
            {t('labels.cancel', 'Cancel')}
          </Button>
          <Button
            onClick={handleSave}
            disabled={isPending}
            className="cursor-pointer"
          >
            {t('labels.save', 'Save')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
