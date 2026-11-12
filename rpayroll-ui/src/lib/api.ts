const rawBaseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined

export const API_BASE_URL = rawBaseUrl?.replace(/\/+$/, '') ?? ''

export async function apiFetch(
  path: string,
  options: RequestInit = {}
): Promise<Response> {
  if (!API_BASE_URL) {
    throw new Error('VITE_API_BASE_URL is not set')
  }

  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  const url = `${API_BASE_URL}${normalizedPath}`

  const headers = new Headers(options.headers || {})
  if (API_BASE_URL.includes('ngrok-free')) {
    headers.set('ngrok-skip-browser-warning', 'true')
  }

  const response = await fetch(url, { ...options, headers })

  if (response.status === 401 && normalizedPath !== '/api/auth/login') {
    try {
      localStorage.removeItem('rpayroll_user')
    } catch {
      // ignore storage errors
    }
    window.location.reload()
  }

  return response
}

export async function readErrorMessage(response: Response): Promise<string> {
  try {
    const data = await response.json()
    if (data && typeof data === 'object') {
      if ('message' in data && typeof data.message === 'string') {
        return data.message
      }
      return JSON.stringify(data)
    }
  } catch {
    // ignore JSON parsing errors
  }
  try {
    const text = await response.text()
    return text || `Request failed (${response.status})`
  } catch {
    return `Request failed (${response.status})`
  }
}
