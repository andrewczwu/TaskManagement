import { apiFetch } from './client'

interface LoginResponse {
  accessToken: string
}

export const register = (email: string, password: string) =>
  apiFetch<void>('/auth/register', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })

export const login = (email: string, password: string) =>
  apiFetch<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })
