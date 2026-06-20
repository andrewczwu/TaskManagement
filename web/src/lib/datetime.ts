// Due dates are date-only. They're stored at UTC midnight and shown in UTC, so the
// calendar day never shifts by timezone.

// Bounds for the date input: years .NET's DateTime can represent. Without an upper
// bound the field accepts 6-digit years, which serialize to a value the API can't parse.
export const DUE_MIN = '1900-01-01'
export const DUE_MAX = '9999-12-31'

// UTC ISO -> value for <input type="date"> (YYYY-MM-DD).
export function toDateInput(utcIso: string | null): string {
  return utcIso ? utcIso.slice(0, 10) : ''
}

// <input type="date"> value -> UTC ISO at midnight, or null if empty.
export function fromDateInput(local: string): string | null {
  return local ? new Date(`${local}T00:00:00.000Z`).toISOString() : null
}

// True when empty, or a real date within the representable range.
export function isValidDueInput(local: string): boolean {
  if (!local) return true
  const d = new Date(`${local}T00:00:00.000Z`)
  const year = d.getUTCFullYear()
  return !Number.isNaN(d.getTime()) && year >= 1900 && year <= 9999
}

// A task is overdue when it isn't complete and its due day is before today.
// Compares calendar days as YYYY-MM-DD strings (the due day in UTC vs today local).
export function isOverdue(utcIso: string | null, isComplete: boolean): boolean {
  if (!utcIso || isComplete) return false
  const dueDay = utcIso.slice(0, 10)
  const now = new Date()
  const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
  return dueDay < today
}

// UTC ISO -> human-friendly date (formatted in UTC so the day doesn't shift).
export function formatDueDate(utcIso: string | null): string {
  if (!utcIso) return 'No due date'
  const date = new Date(utcIso).toLocaleDateString(undefined, {
    timeZone: 'UTC', year: 'numeric', month: 'short', day: 'numeric',
  })
  return `Due ${date}`
}
