import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { apiFetch, readErrorMessage } from '../../lib/api'
import type { FormCanvasItem, FormLayoutConfig } from './types'
import { flattenTabLayouts, normalizeFormLayoutConfig } from './types'
import './FormBuilderUI.css'

type FormRendererProps = {
  config: FormLayoutConfig
  token?: string
  submitEnabled?: boolean
}

export default function FormRenderer({
  config,
  token,
  submitEnabled = true,
}: FormRendererProps) {
  const normalizedConfig = useMemo(() => normalizeFormLayoutConfig(config), [config])
  const [activeTabId, setActiveTabId] = useState(normalizedConfig.tabs[0]?.id ?? 'tab-1')

  useEffect(() => {
    setActiveTabId(normalizedConfig.tabs[0]?.id ?? 'tab-1')
  }, [normalizedConfig])

  const tabs = normalizedConfig.tabs
  const activeTab = tabs.find((tab) => tab.id === activeTabId) ?? tabs[0]
  const { columns, rows } = parseGridSize(activeTab?.gridSize ?? normalizedConfig.gridSize)
  const sortedTabs = useMemo(
    () =>
      tabs.map((tab) => ({
        ...tab,
        layout: [...tab.layout].sort((left, right) => {
          if (left.row === right.row) {
            return left.column - right.column
          }
          return left.row - right.row
        }),
      })),
    [tabs]
  )
  const allItems = useMemo(() => flattenTabLayouts(sortedTabs), [sortedTabs])

  const initialValues = useMemo(() => {
    const next: Record<string, string | boolean> = {}
    for (const item of allItems) {
      next[item.id] = item.type === 'checkbox' ? item.defaultValue === 'true' : item.defaultValue
    }
    return next
  }, [allItems])

  const [values, setValues] = useState<Record<string, string | boolean>>(initialValues)
  const [submitting, setSubmitting] = useState(false)
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)

  useEffect(() => {
    setValues(initialValues)
    setMessage(null)
    setSubmitting(false)
  }, [initialValues])

  const handleChange = (item: FormCanvasItem, value: string | boolean) => {
    setValues((current) => ({ ...current, [item.id]: value }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!submitEnabled) {
      setMessage({ type: 'error', text: 'Submit is disabled in preview mode.' })
      return
    }

    if (!normalizedConfig.submitUrl) {
      setMessage({ type: 'error', text: 'No submit URL configured for this form.' })
      return
    }

    if (!token) {
      setMessage({ type: 'error', text: 'Please sign in again before submitting this form.' })
      return
    }

    setSubmitting(true)
    setMessage(null)

    try {
      const payload = buildPayload(allItems, values)
      const response = await apiFetch(normalizedConfig.submitUrl, {
        method: normalizedConfig.submitMethod ?? 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(payload),
      })

      if (!response.ok) {
        setMessage({ type: 'error', text: await readErrorMessage(response) })
        return
      }

      setMessage({ type: 'success', text: 'Form submitted successfully.' })
      setValues(initialValues)
      setActiveTabId(sortedTabs[0]?.id ?? activeTabId)
    } catch (error) {
      setMessage({
        type: 'error',
        text: error instanceof Error ? error.message : 'Failed to submit form.',
      })
    } finally {
      setSubmitting(false)
    }
  }

  if (!activeTab) {
    return <div className="form-builder__message error">No tabs found for this form.</div>
  }

  return (
    <form className="form-renderer" onSubmit={handleSubmit}>
      {sortedTabs.length > 1 && (
        <div className="form-renderer__tabs-wrapper">
          <div className="form-renderer__tabs">
            {sortedTabs.map((tab) => (
              <button
                key={tab.id}
                type="button"
                className={`form-renderer__tab ${tab.id === activeTab.id ? 'active' : ''}`}
                onClick={() => setActiveTabId(tab.id)}
              >
                {tab.name}
              </button>
            ))}
          </div>
        </div>
      )}

      <div className="form-renderer__panel">
        <div
          className="form-renderer__grid"
          style={{
            gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))`,
            minHeight: `${rows * 88}px`,
          }}
        >
          {activeTab.layout.map((item) => (
            <div
              key={item.id}
              className="form-renderer__field"
              style={{ gridColumn: `span ${Math.min(item.width, columns)}` }}
            >
              <label>{item.label || item.variable || 'Untitled Control'}</label>
              {renderInput(item, values[item.id], (value) => handleChange(item, value))}
            </div>
          ))}
        </div>
      </div>

      {message && (
        <div className={`form-builder__message ${message.type}`}>
          {message.text}
        </div>
      )}

      <div className="form-renderer__actions">
        <button type="submit" disabled={submitting}>
          {submitting ? 'Submitting...' : 'Submit'}
        </button>
      </div>
    </form>
  )
}

export function renderBuilderInput(item: FormCanvasItem) {
  return renderPreviewInput(item, item.defaultValue)
}

function renderInput(
  item: FormCanvasItem,
  value: string | boolean | undefined,
  onChange: (value: string | boolean) => void
) {
  switch (item.type) {
    case 'numeric':
      return (
        <input
          type="number"
          placeholder={item.placeholder}
          value={String(value ?? '')}
          required={item.required}
          onChange={(event) => onChange(event.target.value)}
        />
      )
    case 'dropdown':
    case 'dropdownSearch':
      return (
        <select
          value={String(value ?? '')}
          required={item.required}
          onChange={(event) => onChange(event.target.value)}
        >
          <option value="">Select...</option>
          <option value="option1">Option 1</option>
          <option value="option2">Option 2</option>
        </select>
      )
    case 'date':
      return (
        <input
          type="date"
          value={String(value ?? '')}
          required={item.required}
          onChange={(event) => onChange(event.target.value)}
        />
      )
    case 'dateTime':
      return (
        <input
          type="datetime-local"
          value={String(value ?? '')}
          required={item.required}
          onChange={(event) => onChange(event.target.value)}
        />
      )
    case 'checkbox':
      return (
        <input
          type="checkbox"
          checked={Boolean(value)}
          onChange={(event) => onChange(event.target.checked)}
        />
      )
    case 'textArea':
      return (
        <textarea
          placeholder={item.placeholder}
          value={String(value ?? '')}
          rows={4}
          required={item.required}
          onChange={(event) => onChange(event.target.value)}
        />
      )
    default:
      return (
        <input
          type="text"
          placeholder={item.placeholder}
          value={String(value ?? '')}
          required={item.required}
          onChange={(event) => onChange(event.target.value)}
        />
      )
  }
}

function renderPreviewInput(item: FormCanvasItem, value: string) {
  const commonProps = {
    placeholder: item.placeholder,
    defaultValue: value,
    required: item.required,
  }

  switch (item.type) {
    case 'numeric':
      return <input type="number" {...commonProps} />
    case 'dropdown':
    case 'dropdownSearch':
      return (
        <select defaultValue={value} required={item.required}>
          <option value="">Select...</option>
          <option value="option1">Option 1</option>
          <option value="option2">Option 2</option>
        </select>
      )
    case 'date':
      return <input type="date" defaultValue={value} required={item.required} />
    case 'dateTime':
      return <input type="datetime-local" defaultValue={value} required={item.required} />
    case 'checkbox':
      return <input type="checkbox" defaultChecked={value === 'true'} />
    case 'textArea':
      return <textarea {...commonProps} rows={4} />
    default:
      return <input type="text" {...commonProps} />
  }
}

function buildPayload(layout: FormCanvasItem[], values: Record<string, string | boolean>) {
  const payload: Record<string, unknown> = {}

  for (const item of layout) {
    if (!item.variable) {
      continue
    }

    const value = values[item.id]
    payload[toCamelCase(item.variable)] = normalizeValue(item, value)
  }

  return payload
}

function normalizeValue(item: FormCanvasItem, value: string | boolean | undefined) {
  if (item.type === 'checkbox') {
    return Boolean(value)
  }

  if (item.type === 'numeric') {
    if (value === '' || value == null) {
      return null
    }
    const parsed = Number(value)
    return Number.isNaN(parsed) ? null : parsed
  }

  return value === '' ? null : value
}

function parseGridSize(gridSize: string) {
  const match = gridSize.trim().match(/^(\d+)x(\d+)$/i)
  if (!match) {
    return { columns: 3, rows: 4 }
  }

  const columns = Math.max(1, Number(match[1]) || 3)
  const rows = Math.max(1, Number(match[2]) || 4)
  return { columns, rows }
}

function toCamelCase(value: string) {
  return value.length > 0 ? value.charAt(0).toLowerCase() + value.slice(1) : value
}
