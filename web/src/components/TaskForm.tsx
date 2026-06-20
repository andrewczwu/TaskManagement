import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/client'
import type { Task, TaskInput } from '../api/tasks'
import { fromLocalInput, toLocalInput } from '../lib/datetime'

interface Props {
  initial?: Task
  submitLabel: string
  onSubmit: (input: TaskInput) => Promise<void>
  onCancel?: () => void
}

// Shared by create and edit. State is preserved on error (input is never erased).
export function TaskForm({ initial, submitLabel, onSubmit, onCancel }: Props) {
  const [title, setTitle] = useState(initial?.title ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [due, setDue] = useState(toLocalInput(initial?.dueDate ?? null))
  const [errors, setErrors] = useState<string[]>([])
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setErrors([])
    setSubmitting(true)
    try {
      await onSubmit({
        title,
        description: description.trim() ? description : null,
        dueDate: fromLocalInput(due),
        isComplete: initial?.isComplete ?? false, // edit keeps status; the toggle changes it
      })
      if (!initial) {
        setTitle('')
        setDescription('')
        setDue('')
      }
    } catch (err) {
      setErrors(
        err instanceof ApiError && err.errors
          ? Object.values(err.errors).flat()
          : ['Could not save the task. Please try again.'],
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="form">
      <label>
        Title
        <input value={title} maxLength={200} required
          onChange={(e) => setTitle(e.target.value)} />
      </label>
      <label>
        Description
        <textarea value={description} rows={2} maxLength={2000}
          onChange={(e) => setDescription(e.target.value)} />
      </label>
      <label>
        Due date
        <input type="datetime-local" value={due}
          onChange={(e) => setDue(e.target.value)} />
      </label>
      {errors.length > 0 && (
        <ul className="error" role="alert">{errors.map((m) => <li key={m}>{m}</li>)}</ul>
      )}
      <div className="form-actions">
        <button type="submit" disabled={submitting}>{submitting ? 'Saving…' : submitLabel}</button>
        {onCancel && <button type="button" className="secondary" onClick={onCancel}>Cancel</button>}
      </div>
    </form>
  )
}
