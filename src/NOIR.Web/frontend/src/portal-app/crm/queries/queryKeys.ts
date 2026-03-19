import type { GetContactsParams, GetCompaniesParams, GetLeadsParams, GetActivitiesParams } from '@/types/crm'

export const crmContactKeys = {
  all: ['crm-contacts'] as const,
  lists: () => [...crmContactKeys.all, 'list'] as const,
  list: (params: GetContactsParams) => [...crmContactKeys.lists(), params] as const,
  details: () => [...crmContactKeys.all, 'detail'] as const,
  detail: (id: string) => [...crmContactKeys.details(), id] as const,
}

export const crmCompanyKeys = {
  all: ['crm-companies'] as const,
  lists: () => [...crmCompanyKeys.all, 'list'] as const,
  list: (params: GetCompaniesParams) => [...crmCompanyKeys.lists(), params] as const,
  details: () => [...crmCompanyKeys.all, 'detail'] as const,
  detail: (id: string) => [...crmCompanyKeys.details(), id] as const,
}

export const crmLeadKeys = {
  all: ['crm-leads'] as const,
  lists: () => [...crmLeadKeys.all, 'list'] as const,
  list: (params: GetLeadsParams) => [...crmLeadKeys.lists(), params] as const,
  details: () => [...crmLeadKeys.all, 'detail'] as const,
  detail: (id: string) => [...crmLeadKeys.details(), id] as const,
}

export const crmPipelineKeys = {
  all: ['crm-pipelines'] as const,
  lists: () => [...crmPipelineKeys.all, 'list'] as const,
  views: () => [...crmPipelineKeys.all, 'view'] as const,
  view: (pipelineId: string) => [...crmPipelineKeys.views(), pipelineId] as const,
}

export const crmActivityKeys = {
  all: ['crm-activities'] as const,
  lists: () => [...crmActivityKeys.all, 'list'] as const,
  list: (params: GetActivitiesParams) => [...crmActivityKeys.lists(), params] as const,
}

export const crmDashboardKeys = {
  all: ['crm-dashboard'] as const,
}
