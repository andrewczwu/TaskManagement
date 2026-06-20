// Session persistence in localStorage (see ERD: localStorage + Bearer header).
const TOKEN_KEY = 'tm.token'
const EMAIL_KEY = 'tm.email'

export const getToken = () => localStorage.getItem(TOKEN_KEY)
export const getEmail = () => localStorage.getItem(EMAIL_KEY)

export function setSession(token: string, email: string) {
  localStorage.setItem(TOKEN_KEY, token)
  localStorage.setItem(EMAIL_KEY, email)
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(EMAIL_KEY)
}
