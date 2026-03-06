import { useEffect, useRef, useState } from 'react'
import type { CSSProperties, FormEvent } from 'react'
import HomePage from './components/HomePage'
import { apiFetch } from './lib/api'
import './App.css'

type LoginResponse = {
  token: string
  userId: number
  username: string
  role: string
}

function App() {
  const heroImageUrl = new URL(
    './assets/images/payroll_landing_page.png',
    import.meta.url
  ).toString()
  const hasHero = Boolean(heroImageUrl)
  const [notification, setNotification] = useState<{
    message: string
    type: 'success' | 'error'
    visible: boolean
  } | null>(null)
  const [currentUser, setCurrentUser] = useState<LoginResponse | null>(() => {
    try {
      const stored = localStorage.getItem('rpayroll_user')
      return stored ? (JSON.parse(stored) as LoginResponse) : null
    } catch {
      return null
    }
  })
  const hideTimerRef = useRef<number | null>(null)
  const clearTimerRef = useRef<number | null>(null)

  const showNotification = (message: string, type: 'success' | 'error') => {
    if (hideTimerRef.current) {
      window.clearTimeout(hideTimerRef.current)
    }
    if (clearTimerRef.current) {
      window.clearTimeout(clearTimerRef.current)
    }

    setNotification({ message, type, visible: true })

    hideTimerRef.current = window.setTimeout(() => {
      setNotification((current) =>
        current ? { ...current, visible: false } : current
      )
    }, 9000)

    clearTimerRef.current = window.setTimeout(() => {
      setNotification(null)
    }, 10000)
  }

  const handleLoginSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    const formData = new FormData(event.currentTarget)
    const loginId = String(formData.get('loginId') ?? '')
    const password = String(formData.get('password') ?? '')
    if (!loginId || !password) {
      showNotification('Invalid login credentials', 'error')
      return
    }

    try {
      const response = await apiFetch('/api/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          username: loginId,
          password,
        }),
      })

      if (!response.ok) {
        if (response.status === 401) {
          showNotification('Invalid login credentials', 'error')
          return
        }

        showNotification('Network error', 'error')
        return
      }

      const data = (await response.json()) as LoginResponse
      setCurrentUser(data)
      try {
        localStorage.setItem('rpayroll_user', JSON.stringify(data))
      } catch {
        // ignore storage errors
      }
      showNotification('Login successful', 'success')
    } catch {
      showNotification('Network error', 'error')
    }
  }

  const handleLogout = async () => {
    try {
      if (currentUser?.token) {
        await apiFetch('/api/auth/logout', {
          method: 'POST',
          headers: {
            Authorization: `Bearer ${currentUser.token}`,
          },
        })
      }
    } catch {
      // ignore logout errors
    } finally {
      setCurrentUser(null)
      try {
        localStorage.removeItem('rpayroll_user')
      } catch {
        // ignore storage errors
      }
    }
  }

  return (
    <div
      className={`page ${hasHero ? 'has-hero' : ''}`}
      style={
        hasHero
          ? ({ ['--hero-image' as string]: `url(${heroImageUrl})` } as CSSProperties)
          : undefined
      }
    >
      {notification && (
        <div
          className={`notification ${notification.type} ${
            notification.visible ? 'show' : ''
          }`}
        >
          {notification.message}
        </div>
      )}

      {currentUser ? (
        <HomePage
          token={currentUser.token}
          username={currentUser.username}
          onLogout={handleLogout}
        />
      ) : (
        <section className="left-panel">
          <div className="brand">
            <span className="brand-mark" aria-hidden="true">
              RP
            </span>
          <div className="brand-text">
            <p className="brand-title">RPayroll</p>
            <p className="brand-subtitle">Payroll operations, simplified.</p>
          </div>
        </div>

        <div className="login-card">
          <div className="login-header">
            <p className="eyebrow">Welcome back</p>
            <h1>Sign in to your account</h1>
            <p className="hint">Use your login ID and password to continue.</p>
          </div>

          <form className="login-form" onSubmit={handleLoginSubmit}>
            <label className="field">
              <span className="field-label">Login ID</span>
              <input
                type="text"
                name="loginId"
                placeholder="Enter your login ID"
                autoComplete="username"
                required
              />
            </label>

            <label className="field">
              <span className="field-label">Password</span>
              <input
                type="password"
                name="password"
                placeholder="Enter your password"
                autoComplete="current-password"
                required
              />
            </label>

            <button type="submit" className="primary-btn">
              Sign in
            </button>
          </form>
        </div>
        </section>
      )}

      {!currentUser && (
        <section className="right-panel" aria-label="Brand imagery">
          {!hasHero && (
            <div className="image-placeholder">
              <p>Background image area</p>
              <span>Set the link to cover the full screen</span>
            </div>
          )}
        </section>
      )}
    </div>
  )
}

export default App
