import { useEffect, useState } from 'react'
import FormRenderer from '../features/formBuilder/FormRenderer'
import { apiFetch } from '../lib/api'
import type { FormLayoutConfig } from '../features/formBuilder/types'

type DynamicFormPageProps = {
  token: string
  menuKey: string
}

export default function DynamicFormPage({ token, menuKey }: DynamicFormPageProps) {
  const [config, setConfig] = useState<FormLayoutConfig | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    let active = true

    const loadConfig = async () => {
      setLoading(true)
      try {
        const response = await apiFetch(`/api/form-layouts/menu/${menuKey}`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        })

        if (!response.ok) {
          if (active) {
            setConfig(null)
          }
          return
        }

        const data = (await response.json()) as FormLayoutConfig
        if (active) {
          setConfig(data)
        }
      } finally {
        if (active) {
          setLoading(false)
        }
      }
    }

    loadConfig()

    return () => {
      active = false
    }
  }, [menuKey, token])

  if (loading) {
    return <div>Loading form...</div>
  }

  if (!config) {
    return <div>Form layout not found.</div>
  }

  return <FormRenderer config={config} token={token} />
}
