import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'

const PASSWORD_HINT =
  'At least 8 characters, with an uppercase letter, a lowercase letter, and a number.'

export function Register() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [errors, setErrors] = useState<string[]>([])
  const [submitting, setSubmitting] = useState(false)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setErrors([])
    setSubmitting(true)
    try {
      await register(email, password)
      navigate('/')
    } catch (err) {
      // Surface the server's per-rule messages (e.g. password policy, duplicate email).
      const messages =
        err instanceof ApiError && err.errors
          ? Object.values(err.errors).flat()
          : ['Could not create your account. Please try again.']
      setErrors(messages)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="auth">
      <h1>Create account</h1>
      <form onSubmit={onSubmit} className="form">
        <label>
          Email
          <input type="email" value={email} required autoComplete="email"
            onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label>
          Password
          <input type="password" value={password} required autoComplete="new-password"
            onChange={(e) => setPassword(e.target.value)} />
        </label>
        <p className="hint">{PASSWORD_HINT}</p>
        {errors.length > 0 && (
          <ul className="error" role="alert">
            {errors.map((m) => <li key={m}>{m}</li>)}
          </ul>
        )}
        <button type="submit" disabled={submitting}>
          {submitting ? 'Creating…' : 'Create account'}
        </button>
      </form>
      <p>Already have an account? <Link to="/login">Sign in</Link></p>
    </div>
  )
}
