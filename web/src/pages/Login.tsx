import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'

export function Login() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await login(email, password)
      navigate('/')
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 401
          ? 'Invalid email or password.'
          : 'Could not sign in. Please try again.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="auth">
      <h1>Sign in</h1>
      <form onSubmit={onSubmit} className="form">
        <label>
          Email
          <input type="email" value={email} required autoComplete="email"
            onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Password
          <input type="password" value={password} required autoComplete="current-password"
            onChange={(e) => setPassword(e.target.value)} />
        </label>
        {error && <p className="error" role="alert">{error}</p>}
        <button type="submit" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
      <p>No account? <Link to="/register">Create one</Link></p>
    </div>
  )
}
