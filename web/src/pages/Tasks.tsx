import { useAuth } from '../auth/useAuth'

// Placeholder shell; the task list and forms arrive in the next commit.
export function Tasks() {
  const { email, logout } = useAuth()

  return (
    <div className="container">
      <header className="topbar">
        <h1>My Tasks</h1>
        <div className="topbar-user">
          <span>{email}</span>
          <button type="button" onClick={logout}>Sign out</button>
        </div>
      </header>
      <p>Task management UI is added in the next commit.</p>
    </div>
  )
}
