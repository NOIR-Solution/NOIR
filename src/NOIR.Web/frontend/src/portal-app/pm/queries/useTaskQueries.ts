import { useQuery } from '@tanstack/react-query'
import { getKanbanBoard, getTaskById, getProjectLabels, getArchivedTasks } from '@/services/pm'
import { pmBoardKeys, pmTaskKeys, pmLabelKeys } from './queryKeys'

export const useKanbanBoardQuery = (projectId: string | undefined) =>
  useQuery({
    queryKey: pmBoardKeys.board(projectId!),
    queryFn: () => getKanbanBoard(projectId!),
    enabled: !!projectId,
  })

export const useTaskQuery = (taskId: string | undefined) =>
  useQuery({
    queryKey: pmTaskKeys.detail(taskId!),
    queryFn: () => getTaskById(taskId!),
    enabled: !!taskId,
  })

export const useProjectLabelsQuery = (projectId: string | undefined) =>
  useQuery({
    queryKey: pmLabelKeys.byProject(projectId!),
    queryFn: () => getProjectLabels(projectId!),
    enabled: !!projectId,
  })

export const useArchivedTasksQuery = (projectId: string | undefined) =>
  useQuery({
    queryKey: [...pmTaskKeys.all, 'archived', projectId!],
    queryFn: () => getArchivedTasks(projectId!),
    enabled: !!projectId,
  })
