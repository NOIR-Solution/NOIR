import { apiClient } from './apiClient'
import type {
  ProjectDto,
  ProjectPagedResult,
  CreateProjectRequest,
  UpdateProjectRequest,
  GetProjectsParams,
  ProjectMemberDto,
  AddProjectMemberRequest,
  ProjectMemberRole,
  KanbanBoardDto,
  TaskDto,
  ArchivedTaskCardDto,
  CreateTaskRequest,
  UpdateTaskRequest,
  MoveTaskRequest,
  TaskCommentDto,
  AddTaskCommentRequest,
  TaskLabelDto,
  CreateTaskLabelRequest,
  ProjectColumnDto,
  CreateColumnRequest,
  UpdateColumnRequest,
  ReorderColumnsRequest,
} from '@/types/pm'

// ─── Project endpoints ──────────────────────────────────────────────────────

export const getProjects = async (params: GetProjectsParams = {}): Promise<ProjectPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.search) queryParams.append('search', params.search)
  if (params.status) queryParams.append('status', params.status)

  const query = queryParams.toString()
  return apiClient<ProjectPagedResult>(`/pm/projects${query ? `?${query}` : ''}`)
}

export const getProjectById = async (id: string): Promise<ProjectDto> => {
  return apiClient<ProjectDto>(`/pm/projects/${id}`)
}

export const getProjectByCode = async (code: string): Promise<ProjectDto> => {
  return apiClient<ProjectDto>(`/pm/projects/code/${code}`)
}

export const createProject = async (request: CreateProjectRequest): Promise<ProjectDto> => {
  return apiClient<ProjectDto>('/pm/projects', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateProject = async (id: string, request: UpdateProjectRequest): Promise<ProjectDto> => {
  return apiClient<ProjectDto>(`/pm/projects/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const archiveProject = async (id: string): Promise<void> => {
  await apiClient(`/pm/projects/${id}/archive`, {
    method: 'POST',
  })
}

export const deleteProject = async (id: string): Promise<void> => {
  await apiClient(`/pm/projects/${id}`, {
    method: 'DELETE',
  })
}

// ─── Member endpoints ───────────────────────────────────────────────────────

export const addProjectMember = async (projectId: string, request: AddProjectMemberRequest): Promise<ProjectMemberDto> => {
  return apiClient<ProjectMemberDto>(`/pm/projects/${projectId}/members`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const removeProjectMember = async (projectId: string, memberId: string): Promise<void> => {
  await apiClient(`/pm/projects/${projectId}/members/${memberId}`, {
    method: 'DELETE',
  })
}

export const changeProjectMemberRole = async (projectId: string, memberId: string, role: ProjectMemberRole): Promise<ProjectMemberDto> => {
  return apiClient<ProjectMemberDto>(`/pm/projects/${projectId}/members/${memberId}/role`, {
    method: 'PUT',
    body: JSON.stringify({ role }),
  })
}

// ─── Kanban Board endpoints ─────────────────────────────────────────────────

export const getKanbanBoard = async (projectId: string): Promise<KanbanBoardDto> => {
  return apiClient<KanbanBoardDto>(`/pm/projects/${projectId}/board`)
}

// ─── Task endpoints ─────────────────────────────────────────────────────────

export const createTask = async (request: CreateTaskRequest): Promise<TaskDto> => {
  return apiClient<TaskDto>('/pm/tasks', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const getTaskById = async (id: string): Promise<TaskDto> => {
  return apiClient<TaskDto>(`/pm/tasks/${id}`)
}

export const updateTask = async (id: string, request: UpdateTaskRequest): Promise<TaskDto> => {
  return apiClient<TaskDto>(`/pm/tasks/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const moveTask = async (id: string, request: MoveTaskRequest): Promise<void> => {
  await apiClient(`/pm/tasks/${id}/move`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const deleteTask = async (id: string): Promise<void> => {
  await apiClient(`/pm/tasks/${id}`, {
    method: 'DELETE',
  })
}

export const changeTaskStatus = async (id: string, status: string): Promise<TaskDto> => {
  return apiClient<TaskDto>(`/pm/tasks/${id}/status`, {
    method: 'POST',
    body: JSON.stringify({ status }),
  })
}

export const archiveTask = async (id: string): Promise<string> => {
  return apiClient<string>(`/pm/tasks/${id}/archive`, { method: 'POST' })
}

export const restoreTask = async (id: string): Promise<string> => {
  return apiClient<string>(`/pm/tasks/${id}/restore`, { method: 'POST' })
}

export const permanentDeleteTask = async (id: string): Promise<string> => {
  return apiClient<string>(`/pm/tasks/${id}/permanent`, { method: 'DELETE' })
}

export const getArchivedTasks = async (projectId: string): Promise<ArchivedTaskCardDto[]> => {
  return apiClient<ArchivedTaskCardDto[]>(`/pm/tasks/archived?projectId=${projectId}`)
}

export const emptyProjectTrash = async (projectId: string): Promise<number> => {
  return apiClient<number>(`/pm/tasks/archived?projectId=${projectId}`, { method: 'DELETE' })
}

// ─── Comment endpoints ──────────────────────────────────────────────────────

export const addTaskComment = async (taskId: string, request: AddTaskCommentRequest): Promise<TaskCommentDto> => {
  return apiClient<TaskCommentDto>(`/pm/tasks/${taskId}/comments`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const deleteTaskComment = async (taskId: string, commentId: string): Promise<void> => {
  await apiClient(`/pm/tasks/${taskId}/comments/${commentId}`, {
    method: 'DELETE',
  })
}

// ─── Label endpoints ────────────────────────────────────────────────────────

export const getProjectLabels = async (projectId: string): Promise<TaskLabelDto[]> => {
  return apiClient<TaskLabelDto[]>(`/pm/projects/${projectId}/labels`)
}

export const createTaskLabel = async (projectId: string, request: CreateTaskLabelRequest): Promise<TaskLabelDto> => {
  return apiClient<TaskLabelDto>(`/pm/projects/${projectId}/labels`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const deleteTaskLabel = async (projectId: string, labelId: string): Promise<void> => {
  await apiClient(`/pm/projects/${projectId}/labels/${labelId}`, {
    method: 'DELETE',
  })
}

export const addLabelToTask = async (taskId: string, labelId: string): Promise<void> => {
  await apiClient(`/pm/tasks/${taskId}/labels/${labelId}`, {
    method: 'POST',
  })
}

export const removeLabelFromTask = async (taskId: string, labelId: string): Promise<void> => {
  await apiClient(`/pm/tasks/${taskId}/labels/${labelId}`, {
    method: 'DELETE',
  })
}

// ─── Column endpoints ───────────────────────────────────────────────────────

export const createColumn = async (projectId: string, request: CreateColumnRequest): Promise<ProjectColumnDto> => {
  return apiClient<ProjectColumnDto>(`/pm/projects/${projectId}/columns`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateColumn = async (projectId: string, columnId: string, request: UpdateColumnRequest): Promise<ProjectColumnDto> => {
  return apiClient<ProjectColumnDto>(`/pm/projects/${projectId}/columns/${columnId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const reorderColumns = async (projectId: string, request: ReorderColumnsRequest): Promise<void> => {
  await apiClient(`/pm/projects/${projectId}/columns/reorder`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const deleteColumn = async (projectId: string, columnId: string, moveToColumnId: string): Promise<void> => {
  await apiClient(`/pm/projects/${projectId}/columns/${columnId}?moveToColumnId=${moveToColumnId}`, {
    method: 'DELETE',
  })
}
