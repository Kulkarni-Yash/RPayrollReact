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

  return fetch(url, options)
}
