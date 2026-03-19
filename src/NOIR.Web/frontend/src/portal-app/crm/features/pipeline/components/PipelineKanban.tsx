import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Kanban } from 'lucide-react'
import { KanbanBoard, EmptyState, type KanbanColumnDef, type KanbanMoveCardParams, type KanbanTerminateCardParams } from '@uikit'
import type { PipelineViewDto, LeadCardDto, StageWithLeadsDto } from '@/types/crm'
import { useMoveLeadStage, useWinLead, useLoseLead } from '@/portal-app/crm/queries'
import { LeadCard } from './LeadCard'
import { StageColumnHeader } from './StageColumnHeader'

interface PipelineKanbanProps {
  pipelineView: PipelineViewDto | undefined
  isLoading: boolean
  onLeadClick: (lead: LeadCardDto) => void
}

/** Map a pipeline stage to a KanbanColumnDef for the generic board. */
const stageToColumn = (stage: StageWithLeadsDto): KanbanColumnDef<LeadCardDto> => ({
  id: stage.id,
  cards: stage.leads,
  isSystem: stage.isSystem,
  systemType: stage.isSystem ? stage.stageType.toLowerCase() : undefined,
})

export const PipelineKanban = ({ pipelineView, isLoading, onLeadClick }: PipelineKanbanProps) => {
  const { t } = useTranslation('common')
  const moveLeadStageMutation = useMoveLeadStage()
  const winLeadMutation = useWinLead()
  const loseLeadMutation = useLoseLead()

  const columns = pipelineView?.stages.map(stageToColumn) ?? []

  const handleMoveCard = useCallback(({ cardId, toColumnId, prevCardId, nextCardId }: KanbanMoveCardParams) => {
    if (!pipelineView) return

    // Calculate new sort order from neighbors using server data
    const targetStage = pipelineView.stages.find(s => s.id === toColumnId)
    if (!targetStage) return

    const stageLeads = targetStage.leads.filter(l => l.id !== cardId)
    const prevLead = prevCardId ? stageLeads.find(l => l.id === prevCardId) : null
    const nextLead = nextCardId ? stageLeads.find(l => l.id === nextCardId) : null

    let newSortOrder: number
    if (!prevLead && !nextLead) {
      newSortOrder = 1
    } else if (!prevLead) {
      newSortOrder = nextLead!.sortOrder > 0 ? nextLead!.sortOrder / 2 : nextLead!.sortOrder - 1
    } else if (!nextLead) {
      newSortOrder = prevLead.sortOrder + 1
    } else {
      const prev = prevLead.sortOrder
      const next = nextLead.sortOrder
      newSortOrder = prev < next ? (prev + next) / 2 : prev + 1
    }

    moveLeadStageMutation.mutate(
      { leadId: cardId, newStageId: toColumnId, newSortOrder },
      { onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')) },
    )
  }, [pipelineView, moveLeadStageMutation, t])

  const handleTerminateCard = useCallback(({ cardId, systemType }: KanbanTerminateCardParams) => {
    if (systemType === 'won') {
      winLeadMutation.mutate(cardId, {
        onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')),
      })
    } else if (systemType === 'lost') {
      loseLeadMutation.mutate(
        { id: cardId },
        { onError: (err) => toast.error(err instanceof Error ? err.message : t('errors.unknown')) },
      )
    }
  }, [winLeadMutation, loseLeadMutation, t])

  const renderColumnHeader = useCallback((column: KanbanColumnDef<LeadCardDto>) => {
    const stage = pipelineView?.stages.find(s => s.id === column.id)
    if (!stage) return null
    return (
      <StageColumnHeader
        name={stage.name}
        color={stage.color}
        totalValue={stage.totalValue}
        leadCount={stage.leadCount}
      />
    )
  }, [pipelineView])

  const renderCard = useCallback((lead: LeadCardDto) => (
    <LeadCard lead={lead} onClick={onLeadClick} />
  ), [onLeadClick])

  return (
    <KanbanBoard
      columns={columns}
      getCardId={(lead) => lead.id}
      renderCard={renderCard}
      renderColumnHeader={renderColumnHeader}
      onMoveCard={handleMoveCard}
      onTerminateCard={handleTerminateCard}
      isLoading={isLoading}
      emptyState={
        <EmptyState
          icon={Kanban}
          title={t('crm.pipeline.noPipelines')}
          description={t('crm.pipeline.noPipelinesDescription')}
        />
      }
    />
  )
}
