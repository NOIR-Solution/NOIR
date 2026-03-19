import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { getLeads, getLeadById, getPipelineView } from '@/services/crm'
import type { GetLeadsParams } from '@/types/crm'
import { crmLeadKeys, crmPipelineKeys } from './queryKeys'

export const useLeadsQuery = (params: GetLeadsParams) =>
  useQuery({
    queryKey: crmLeadKeys.list(params),
    queryFn: () => getLeads(params),
    placeholderData: keepPreviousData,
  })

export const useLeadQuery = (id: string | undefined) =>
  useQuery({
    queryKey: crmLeadKeys.detail(id!),
    queryFn: () => getLeadById(id!),
    enabled: !!id,
  })

export const usePipelineViewQuery = (pipelineId: string | undefined) =>
  useQuery({
    queryKey: crmPipelineKeys.view(pipelineId!),
    queryFn: () => getPipelineView(pipelineId!),
    enabled: !!pipelineId,
  })
