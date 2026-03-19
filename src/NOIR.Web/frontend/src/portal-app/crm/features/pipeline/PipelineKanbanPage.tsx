import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Button,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'
import { usePipelinesQuery, usePipelineViewQuery } from '@/portal-app/crm/queries'
import type { LeadCardDto } from '@/types/crm'
import { PipelineKanban } from './components/PipelineKanban'

export const PipelineKanbanPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  usePageContext('CRM Pipeline')

  const canCreateLead = hasPermission(Permissions.CrmLeadsCreate)

  const { data: pipelines } = usePipelinesQuery()
  const [selectedPipelineId, setSelectedPipelineId] = useState<string>('')

  // Auto-select default pipeline
  useEffect(() => {
    if (pipelines && pipelines.length > 0 && !selectedPipelineId) {
      const defaultPipeline = pipelines.find(p => p.isDefault) || pipelines[0]
      setSelectedPipelineId(defaultPipeline.id)
    }
  }, [pipelines, selectedPipelineId])

  const { data: pipelineView, isLoading: viewLoading } = usePipelineViewQuery(
    selectedPipelineId || undefined,
  )

  const handleLeadClick = (lead: LeadCardDto) => {
    navigate(`/portal/crm/pipeline/deals/${lead.id}`)
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{t('crm.pipeline.title')}</h1>
          <p className="text-muted-foreground">{t('crm.pipeline.description')}</p>
        </div>
        <div className="flex items-center gap-3">
          {/* Pipeline selector */}
          {pipelines && pipelines.length > 1 && (
            <Select value={selectedPipelineId} onValueChange={setSelectedPipelineId}>
              <SelectTrigger className="w-[200px] cursor-pointer" aria-label={t('crm.pipeline.selectPipeline')}>
                <SelectValue placeholder={t('crm.pipeline.selectPipeline')} />
              </SelectTrigger>
              <SelectContent>
                {pipelines.map((pipeline) => (
                  <SelectItem key={pipeline.id} value={pipeline.id} className="cursor-pointer">
                    {pipeline.name}
                    {pipeline.isDefault && ` (${t('crm.pipeline.isDefault')})`}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}

          {canCreateLead && (
            <Button className="group transition-all duration-300 cursor-pointer" onClick={() => navigate('/portal/crm/pipeline?dialog=create-crm-lead')}>
              <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
              {t('crm.leads.create')}
            </Button>
          )}
        </div>
      </div>

      <PipelineKanban
        pipelineView={pipelineView}
        isLoading={viewLoading}
        onLeadClick={handleLeadClick}
      />
    </div>
  )
}

export default PipelineKanbanPage
