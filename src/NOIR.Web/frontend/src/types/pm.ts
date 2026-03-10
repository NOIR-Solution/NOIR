// ─── Enums ──────────────────────────────────────────────────────────────────

export type ProjectStatus = 'Active' | 'Completed' | 'Archived' | 'OnHold'
export type ProjectVisibility = 'Private' | 'Internal' | 'Public'
export type ProjectTaskStatus = 'Todo' | 'InProgress' | 'InReview' | 'Done' | 'Cancelled'
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Urgent'
export type ProjectMemberRole = 'Owner' | 'Manager' | 'Member' | 'Viewer'

// ─── Project DTOs ───────────────────────────────────────────────────────────

export interface ProjectListDto {
  id: string
  name: string
  slug: string
  projectCode: string
  status: ProjectStatus
  startDate: string | null
  endDate: string | null
  dueDate: string | null
  ownerName: string | null
  memberCount: number
  taskCount: number
  completedTaskCount: number
  color: string | null
  icon: string | null
  visibility: ProjectVisibility
  createdAt: string
}

export interface ProjectDto {
  id: string
  name: string
  slug: string
  description: string | null
  status: ProjectStatus
  startDate: string | null
  endDate: string | null
  dueDate: string | null
  ownerId: string | null
  ownerName: string | null
  budget: number | null
  currency: string | null
  color: string | null
  icon: string | null
  visibility: ProjectVisibility
  members: ProjectMemberDto[]
  columns: ProjectColumnDto[]
  projectCode: string
  createdAt: string
  modifiedAt: string | null
}

export interface ProjectMemberDto {
  id: string
  employeeId: string
  employeeName: string
  avatarUrl: string | null
  role: ProjectMemberRole
  joinedAt: string
}

export interface ProjectColumnDto {
  id: string
  name: string
  sortOrder: number
  color: string | null
  wipLimit: number | null
}

// ─── Task DTOs ──────────────────────────────────────────────────────────────

export interface TaskCardDto {
  id: string
  taskNumber: string
  title: string
  status: ProjectTaskStatus
  priority: TaskPriority
  assigneeName: string | null
  assigneeAvatarUrl: string | null
  dueDate: string | null
  commentCount: number
  subtaskCount: number
  completedSubtaskCount: number
  labels: TaskLabelBriefDto[]
  sortOrder: number
  parentTaskId: string | null
  parentTaskNumber: string | null
  reporterName: string | null
  reporterAvatarUrl: string | null
  completedAt: string | null
}

export interface TaskDto {
  id: string
  projectId: string
  taskNumber: string
  title: string
  description: string | null
  status: ProjectTaskStatus
  priority: TaskPriority
  assigneeId: string | null
  assigneeName: string | null
  reporterId: string | null
  reporterName: string | null
  dueDate: string | null
  estimatedHours: number | null
  actualHours: number | null
  parentTaskId: string | null
  parentTaskNumber: string | null
  parentTaskTitle: string | null
  columnId: string | null
  columnName: string | null
  completedAt: string | null
  isArchived: boolean
  archivedAt: string | null
  labels: TaskLabelBriefDto[]
  subtasks: SubtaskDto[]
  comments: TaskCommentDto[]
  createdAt: string
  modifiedAt: string | null
  projectName: string | null
  assigneeAvatarUrl: string | null
  projectCode: string | null
}

export interface ArchivedTaskCardDto {
  id: string
  taskNumber: string
  dueDate: string | null
  title: string
  status: ProjectTaskStatus
  priority: TaskPriority
  assigneeName: string | null
  assigneeAvatarUrl: string | null
  archivedAt: string | null
  subtaskCount: number
  labels: TaskLabelBriefDto[]
  parentTaskId: string | null
  parentTaskNumber: string | null
}

export interface SubtaskDto {
  id: string
  taskNumber: string
  title: string
  status: ProjectTaskStatus
  priority: TaskPriority
  assigneeName: string | null
  columnName: string | null
}

export interface TaskCommentDto {
  id: string
  authorId: string
  authorName: string
  authorAvatarUrl: string | null
  content: string
  isEdited: boolean
  createdAt: string
}

export interface TaskLabelDto {
  id: string
  name: string
  color: string
}

export interface TaskLabelBriefDto {
  id: string
  name: string
  color: string
}

// ─── Kanban Board ───────────────────────────────────────────────────────────

export interface KanbanBoardDto {
  projectId: string
  projectName: string
  columns: KanbanColumnDto[]
}

export interface KanbanColumnDto {
  id: string
  name: string
  sortOrder: number
  color: string | null
  wipLimit: number | null
  tasks: TaskCardDto[]
}

// ─── Request Types ──────────────────────────────────────────────────────────

export interface GetProjectsParams {
  page?: number
  pageSize?: number
  search?: string
  status?: ProjectStatus
}

export interface CreateProjectRequest {
  name: string
  description?: string
  startDate?: string
  endDate?: string
  dueDate?: string
  budget?: number
  currency?: string
  color?: string
  icon?: string
  visibility?: ProjectVisibility
}

export interface UpdateProjectRequest extends CreateProjectRequest {
  status?: ProjectStatus
}

export interface CreateTaskRequest {
  projectId: string
  title: string
  description?: string
  priority?: TaskPriority
  assigneeId?: string
  dueDate?: string
  estimatedHours?: number
  parentTaskId?: string
  columnId?: string
}

export interface UpdateTaskRequest {
  title?: string
  description?: string
  priority?: TaskPriority
  assigneeId?: string
  dueDate?: string
  estimatedHours?: number
  actualHours?: number
}

export interface MoveTaskRequest {
  columnId: string
  sortOrder: number
}

export interface AddProjectMemberRequest {
  employeeId: string
  role: ProjectMemberRole
}

export interface AddTaskCommentRequest {
  content: string
}

export interface CreateTaskLabelRequest {
  name: string
  color: string
}

export interface CreateColumnRequest {
  name: string
  color?: string
  wipLimit?: number
}

export interface UpdateColumnRequest {
  name?: string
  color?: string
  wipLimit?: number
}

export interface ReorderColumnsRequest {
  columnIds: string[]
}

// ─── Paged Results ──────────────────────────────────────────────────────────

export interface ProjectPagedResult {
  items: ProjectListDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
