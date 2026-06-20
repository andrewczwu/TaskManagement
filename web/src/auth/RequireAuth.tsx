import { type ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from './useAuth'

// Guards routes: unauthenticated visitors are sent to login.
export function RequireAuth({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth()
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />
}
