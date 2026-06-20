import { getToken } from '../auth/token'

const BASE = '/api/v1'

// Carries the HTTP status and any field-level validation errors to the UI.
export class ApiError extends Error {
  status: number
  errors?: Record<string, string[]>
  constructor(status: number, message: string, errors?: Record<string, string[]>) {
    super(message)
    this.status = status
    this.errors = errors
  }
}

// AuthProvider registers a handler so an expired token logs the user out.
let onUnauthorized: (() => void) | null = null
export const setUnauthorizedHandler = (fn: () => void) => { onUnauthorized = fn }

export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken()
  const headers = new Headers(options.headers)
  headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const res = await fetch(`${BASE}${path}`, { ...options, headers })

  // A 401 on an authenticated request means the session expired -> log out.
  // (Login itself sends no token, so its 401 falls through as "bad credentials".)
  if (res.status === 401 && token) {
    onUnauthorized?.()
    throw new ApiError(401, 'Your session has expired. Please sign in again.')
  }

  if (!res.ok) {
    const problem = await res.json().catch(() => null)
    throw new ApiError(res.status, problem?.title ?? 'Something went wrong.', problem?.errors)
  }

  // Some endpoints (e.g. register) return 2xx with an empty body.
  const text = await res.text()
  return (text ? JSON.parse(text) : undefined) as T
}
