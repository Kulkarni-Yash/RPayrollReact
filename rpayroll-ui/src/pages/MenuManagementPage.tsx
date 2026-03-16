import { useEffect, useMemo, useState } from 'react'
import EntityList from '../components/listing/EntityList'
import { apiFetch } from '../lib/api'

type MenuManagementPageProps = {
  token: string
  onMenusChanged?: () => void
  onEditForm?: (layoutKey: string) => void
}

type MenuItem = {
  key: string
  label: string
  order: number
  isActive: boolean
  isSystem: boolean
  formLayoutKey?: string | null
  visibilityScope: number
  maxHierarchyLevelAllowed?: number | null
  ownerUserId?: number | null
}

export default function MenuManagementPage({ token, onMenusChanged, onEditForm }: MenuManagementPageProps) {
  const [items, setItems] = useState<MenuItem[]>([])
  const [loading, setLoading] = useState(false)

  const loadItems = async () => {
    setLoading(true)
    try {
      const response = await apiFetch('/api/menu/manage', {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      })

      if (!response.ok) {
        return
      }

      const data = (await response.json()) as MenuItem[]
      setItems(Array.isArray(data) ? data : [])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadItems()
  }, [token])

  const toggleStatus = async (item: MenuItem) => {
    const response = await apiFetch(`/api/menu/manage/${item.key}/status`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({
        isActive: !item.isActive,
      }),
    })

    if (!response.ok) {
      return
    }

    const updated = (await response.json()) as MenuItem
    setItems((current) => current.map((entry) => (entry.key === updated.key ? updated : entry)))
    onMenusChanged?.()
  }

  const columns = useMemo(
    () => [
      {
        key: 'label',
        header: 'Menu Label',
        sortable: true,
        sortValue: (item: MenuItem) => item.label,
        render: (item: MenuItem) => item.label,
      },
      {
        key: 'key',
        header: 'Key',
        sortable: true,
        sortValue: (item: MenuItem) => item.key,
        render: (item: MenuItem) => item.key,
      },
      {
        key: 'type',
        header: 'Type',
        render: (item: MenuItem) => (item.isSystem ? 'System' : 'Generated Form'),
      },
      {
        key: 'visibility',
        header: 'Visibility',
        render: (item: MenuItem) => {
          if (item.visibilityScope === 1) {
            return `Role <= ${item.maxHierarchyLevelAllowed ?? '-'}`
          }
          if (item.visibilityScope === 2) {
            return 'Only Owner'
          }
          return 'Everyone'
        },
      },
      {
        key: 'status',
        header: 'Status',
        sortable: true,
        sortValue: (item: MenuItem) => (item.isActive ? 'Active' : 'Inactive'),
        render: (item: MenuItem) =>
          item.isActive ? <span className="status-active">Active</span> : <span className="status-inactive">Inactive</span>,
      },
      {
        key: 'actions',
        header: 'Actions',
        render: (item: MenuItem) => (
          <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
            {!item.isSystem && item.formLayoutKey && (
              <button
                type="button"
                className="activate-btn"
                onClick={() => onEditForm?.(item.formLayoutKey!)}
              >
                Edit Form
              </button>
            )}
            <button
              type="button"
              className={item.isActive ? 'deactivate-btn' : 'activate-btn'}
              onClick={() => toggleStatus(item)}
              disabled={item.isSystem && item.isActive}
            >
              {item.isActive ? 'Deactivate' : 'Activate'}
            </button>
          </div>
        ),
      },
    ],
    [token]
  )

  return (
    <EntityList
      title="Menu Items"
      items={items}
      columns={columns}
      loading={loading}
      rowKey={(item) => item.key}
      searchPlaceholder="Search menu items..."
      searchValue={(item) => `${item.label} ${item.key}`}
      emptyMessage="No menu items found"
    />
  )
}
