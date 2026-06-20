import { apiFetch } from './client'

export interface Task {
  id: string
  title: string
  description: string | null
  isComplete: boolean
  dueDate: string | null // UTC ISO
  createdAt: string
  updatedAt: string
}

// PUT is a full replace, so callers always send every editable field.
export interface TaskInput {
  title: string
  description: string | null
  dueDate: string | null
  isComplete: boolean
}

export const getTasks = () => apiFetch<Task[]>('/tasks')

export const createTask = (input: TaskInput) =>
  apiFetch<Task>('/tasks', { method: 'POST', body: JSON.stringify(input) })

export const updateTask = (id: string, input: TaskInput) =>
  apiFetch<Task>(`/tasks/${id}`, { method: 'PUT', body: JSON.stringify(input) })

export const deleteTask = (id: string) =>
  apiFetch<void>(`/tasks/${id}`, { method: 'DELETE' })
