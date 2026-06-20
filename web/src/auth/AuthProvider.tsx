import { useEffect, useState, type ReactNode } from 'react'
import * as authApi from '../api/auth'
import { setUnauthorizedHandler } from '../api/client'
import { AuthContext } from './auth-context'
import { clearSession, getEmail, getToken, setSession } from './token'

export function AuthProvider({ children }: { children: ReactNode }) {
  // Initialize from localStorage so a refresh keeps the session.
  const [email, setEmail] = useState<string | null>(() => (getToken() ? getEmail() : null))

  const logout = () => {
    clearSession()
    setEmail(null)
  }

  // An expired token (401 on an authed request) logs the user out.
  useEffect(() => setUnauthorizedHandler(logout), [])

  const login = async (email: string, password: string) => {
    const { accessToken } = await authApi.login(email, password)
    setSession(accessToken, email)
    setEmail(email)
  }

  const register = async (email: string, password: string) => {
    await authApi.register(email, password)
    await login(email, password) // register returns no token; log in immediately
  }

  return (
    <AuthContext value={{ email, isAuthenticated: email !== null, login, register, logout }}>
      {children}
    </AuthContext>
  )
}
