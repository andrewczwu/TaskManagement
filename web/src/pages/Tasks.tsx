import { useCallback, useEffect, useState } from 'react'
import * as tasksApi from '../api/tasks'
import type { Task, TaskInput } from '../api/tasks'
import { useAuth } from '../auth/useAuth'
import { TaskForm } from '../components/TaskForm'
import { TaskItem } from '../components/TaskItem'

export function Tasks() {
  const { email, logout } = useAuth()
  const [tasks, setTasks] = useState<Task[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [editingId, setEditingId] = useState<string | null>(null)

  // Refetch after every change so the list reflects the server's ordering.
  const load = useCallback(async () => {
    try {
      const data = await tasksApi.getTasks()
      setTasks(data)
      setError(null)
    } catch {
      setError('Could not load your tasks.')
    } finally {
      setLoading(false)
    }
  }, [])

  // Initial load. State is only set after the await (and if still mounted).
  useEffect(() => {
    let active = true
    tasksApi.getTasks()
      .then((data) => { if (active) setTasks(data) })
      .catch(() => { if (active) setError('Could not load your tasks.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  const create = async (input: TaskInput) => {
    await tasksApi.createTask(input)
    await load()
  }

  const update = (id: string) => async (input: TaskInput) => {
    await tasksApi.updateTask(id, input)
    setEditingId(null)
    await load()
  }

  const toggle = async (task: Task) => {
    try {
      await tasksApi.updateTask(task.id, {
        title: task.title,
        description: task.description,
        dueDate: task.dueDate,
        isComplete: !task.isComplete,
      })
      await load()
    } catch {
      setError('Could not update the task.')
    }
  }

  const remove = async (task: Task) => {
    try {
      await tasksApi.deleteTask(task.id)
      await load()
    } catch {
      setError('Could not delete the task.')
    }
  }

  return (
    <div className="container">
      <header className="topbar">
        <h1>My Tasks</h1>
        <div className="topbar-user">
          <span>{email}</span>
          <button type="button" onClick={logout}>Sign out</button>
        </div>
      </header>

      <section className="card">
        <h2>Add a task</h2>
        <TaskForm submitLabel="Add task" onSubmit={create} />
      </section>

      {loading && <p>Loading…</p>}
      {error && <p className="error" role="alert">{error}</p>}
      {!loading && !error && tasks.length === 0 && (
        <p className="empty">No tasks yet. Add your first one above.</p>
      )}

      <ul className="task-list">
        {tasks.map((task) =>
          task.id === editingId ? (
            <li key={task.id} className="task editing">
              <TaskForm initial={task} submitLabel="Save"
                onSubmit={update(task.id)} onCancel={() => setEditingId(null)} />
            </li>
          ) : (
            <TaskItem key={task.id} task={task} onToggle={toggle}
              onEdit={() => setEditingId(task.id)} onDelete={remove} />
          ),
        )}
      </ul>
    </div>
  )
}
