import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createTask, updateTask, moveTask, deleteTask, changeTaskStatus, archiveTask, restoreTask, permanentDeleteTask, emptyProjectTrash } from '@/services/pm'
import type { CreateTaskRequest, UpdateTaskRequest, MoveTaskRequest } from '@/types/pm'
import { pmBoardKeys, pmTaskKeys, pmProjectKeys } from './queryKeys'

export const useCreateTask = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateTaskRequest) => createTask(request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: pmBoardKeys.board(variables.projectId) })
      queryClient.invalidateQueries({ queryKey: pmProjectKeys.all })
    },
  })
}

export const useUpdateTask = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateTaskRequest }) => updateTask(id, request),
    onSuccess: (data) => {
      queryClient.setQueryData(pmTaskKeys.detail(data.id), data)
      queryClient.invalidateQueries({ queryKey: pmBoardKeys.all })
    },
  })
}

export const useMoveTask = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: MoveTaskRequest }) => moveTask(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pmBoardKeys.all })
    },
  })
}

export const useDeleteTask = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteTask(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pmTaskKeys.all })
      queryClient.invalidateQueries({ queryKey: pmBoardKeys.all })
      queryClient.invalidateQueries({ queryKey: pmProjectKeys.all })
    },
  })
}

export const useChangeTaskStatus = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) => changeTaskStatus(id, status),
    onSuccess: (data) => {
      queryClient.setQueryData(pmTaskKeys.detail(data.id), data)
      queryClient.invalidateQueries({ queryKey: pmBoardKeys.all })
    },
  })
}

export const useArchiveTask = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => archiveTask(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pmBoardKeys.all })
      queryClient.invalidateQueries({ queryKey: pmTaskKeys.all })
    },
  })
}

export const useRestoreTask = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => restoreTask(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pmBoardKeys.all })
      queryClient.invalidateQueries({ queryKey: pmTaskKeys.all })
    },
  })
}

export const usePermanentDeleteTask = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => permanentDeleteTask(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pmTaskKeys.all })
      queryClient.invalidateQueries({ queryKey: pmBoardKeys.all })
    },
  })
}

export const useEmptyProjectTrash = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (projectId: string) => emptyProjectTrash(projectId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pmTaskKeys.all })
    },
  })
}
