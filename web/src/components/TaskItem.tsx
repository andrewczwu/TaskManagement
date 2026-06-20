import { useState } from 'react'
import type { Task } from '../api/tasks'
import { formatDueDate } from '../lib/datetime'

interface Props {
  task: Task
  onToggle: (task: Task) => void
  onEdit: () => void
  onDelete: (task: Task) => void
}

export function TaskItem({ task, onToggle, onEdit, onDelete }: Props) {
  const [confirming, setConfirming] = useState(false)

  return (
    <li className={`task ${task.isComplete ? 'done' : ''}`}>
      <input type="checkbox" checked={task.isComplete}
        onChange={() => onToggle(task)} aria-label="Toggle complete" />

      <div className="task-body">
        <span className="task-title">{task.title}</span>
        {task.description && <p className="task-desc">{task.description}</p>}
        <span className="task-due">{formatDueDate(task.dueDate)}</span>
      </div>

      <div className="task-actions">
        {confirming ? (
          <>
            <span>Delete?</span>
            <button type="button" className="danger" onClick={() => onDelete(task)}>Yes</button>
            <button type="button" className="secondary" onClick={() => setConfirming(false)}>No</button>
          </>
        ) : (
          <>
            <button type="button" className="secondary" onClick={onEdit}>Edit</button>
            <button type="button" className="danger" onClick={() => setConfirming(true)}>Delete</button>
          </>
        )}
      </div>
    </li>
  )
}
