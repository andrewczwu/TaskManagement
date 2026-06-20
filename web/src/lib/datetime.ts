// Dates cross the wire as UTC ISO strings; the UI works in the user's local time.

const pad = (n: number) => String(n).padStart(2, '0')

// UTC ISO -> value for <input type="datetime-local"> (local time).
export function toLocalInput(utcIso: string | null): string {
  if (!utcIso) return ''
  const d = new Date(utcIso)
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

// datetime-local value (local time) -> UTC ISO string, or null if empty.
export function fromLocalInput(local: string): string | null {
  return local ? new Date(local).toISOString() : null
}

// UTC ISO -> human-friendly local string.
export function formatDueDate(utcIso: string | null): string {
  return utcIso ? `Due ${new Date(utcIso).toLocaleString()}` : 'No due date'
}
