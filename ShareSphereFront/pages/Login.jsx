import React, { useState } from 'react'

export default function Login() {
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState(null)

  async function handleSubmit(e) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userName, password })
      })

      if (!res.ok) {
        // Versuche, eine sinnvolle Fehlermeldung aus JSON oder Text zu extrahieren
        let message = `Login fehlgeschlagen (HTTP ${res.status})`
        try {
          const json = await res.json()
          if (json) {
            message = json.message || json.error || JSON.stringify(json)
          }
        } catch {
          const txt = await res.text()
          if (txt) message = txt
        }
        throw new Error(message)
      }

      // Response als JSON parsen (kann auch leer sein)
      let data = null
      try {
        data = await res.json()
      } catch {
        data = null
      }

      if (data && data.token) {
        localStorage.setItem('auth_token', data.token)
      } else {
        // Falls kein Token geliefert wird, trotzdem weiterleiten oder anders behandeln
        console.warn('Login: Keine token-Eigenschaft in der Antwort', data)
      }

      window.location.href = '/'
    } catch (err) {
      setError(err?.message || 'Unbekannter Fehler')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div style={{ maxWidth: 360, margin: '2rem auto' }}>
      <h1>Login</h1>
      <form onSubmit={handleSubmit}>
        <label style={{ display: 'block', marginBottom: 8 }}>
          Benutzername
          <input
            type="text"
            value={userName}
            onChange={e => setUserName(e.target.value)}
            autoComplete="username"
            required
            disabled={busy}
            style={{ width: '100%', padding: 8 }}
          />
        </label>

        <label style={{ display: 'block', marginBottom: 12 }}>
          Passwort
          <input
            type="password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            autoComplete="current-password"
            required
            disabled={busy}
            style={{ width: '100%', padding: 8 }}
          />
        </label>

        <button
          type="submit"
          disabled={busy}
          style={{ width: '100%', padding: 10 }}
          aria-busy={busy}
        >
          {busy ? 'Wird angemeldet…' : 'Login'}
        </button>
      </form>

      {error && (
        <p role="alert" style={{ color: 'crimson', marginTop: 12 }}>
          {error}
        </p>
      )}
    </div>
  )
}