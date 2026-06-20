import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/client'
import type { Task, TaskInput } from '../api/tasks'
import { DUE_MAX, DUE_MIN, fromDateInput, isValidDueInput, toDateInput } from '../lib/datetime'

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
  const [due, setDue] = useState(toDateInput(initial?.dueDate ?? null))
  const [errors, setErrors] = useState<string[]>([])
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setErrors([])
    if (!isValidDueInput(due)) {
      setErrors(['Please enter a valid due date.'])
      return
    }
    setSubmitting(true)
    try {
      await onSubmit({
        title,
        description: description.trim() ? description : null,
        dueDate: fromDateInput(due),
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
        <span>Title <span className="req">*</span></span>
        <input value={title} maxLength={200} required
          onChange={(e) => setTitle(e.target.value)} />
      </label>
      <label>
        <span>Description <span className="optional">(optional)</span></span>
        <textarea value={description} rows={2} maxLength={2000}
          onChange={(e) => setDescription(e.target.value)} />
      </label>
      <label>
        <span>Due date <span className="optional">(optional)</span></span>
        <input type="date" value={due} min={DUE_MIN} max={DUE_MAX}
          onChange={(e) => setDue(e.target.value)} />
      </label>
      <p className="hint"><span className="req">*</span> required</p>
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
