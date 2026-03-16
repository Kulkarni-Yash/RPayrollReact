import { useMemo, useState } from "react"
import type { ReactNode } from "react"
import "./EntityList.css"

export type EntityListColumn<T> = {
  key: string
  header: string
  sortable?: boolean
  render: (item: T) => ReactNode
  sortValue?: (item: T) => string | number
}

type EntityListProps<T> = {
  title?: string
  items: T[]
  columns: EntityListColumn<T>[]
  searchPlaceholder?: string
  searchValue?: (item: T) => string
  rowKey: (item: T) => string | number
  loading?: boolean
  emptyMessage?: string
}

export default function EntityList<T>({
  title,
  items,
  columns,
  searchPlaceholder = "Search...",
  searchValue,
  rowKey,
  loading = false,
  emptyMessage = "No records found"
}: EntityListProps<T>) {
  const sortableColumns = columns.filter((column) => column.sortable)
  const defaultSortColumn = sortableColumns[0]?.key ?? columns[0]?.key ?? ""

  const [search, setSearch] = useState("")
  const [sortField, setSortField] = useState(defaultSortColumn)
  const [page, setPage] = useState(1)

  const pageSize = 5

  const filteredItems = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase()

    const filtered = items.filter((item) => {
      if (!normalizedSearch || !searchValue) {
        return true
      }

      return searchValue(item).toLowerCase().includes(normalizedSearch)
    })

    const activeColumn = columns.find((column) => column.key === sortField)
    if (!activeColumn?.sortable) {
      return filtered
    }

    return [...filtered].sort((left, right) => {
      const leftValue = String(activeColumn.sortValue?.(left) ?? "")
      const rightValue = String(activeColumn.sortValue?.(right) ?? "")
      return leftValue.localeCompare(rightValue)
    })
  }, [columns, items, search, searchValue, sortField])

  const totalPages = Math.max(1, Math.ceil(filteredItems.length / pageSize))
  const currentPage = Math.min(page, totalPages)
  const paginatedItems = filteredItems.slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize
  )

  return (
    <div className="entity-list">
      <div className="entity-list__toolbar">
        <div>
          {title && <h3 className="entity-list__title">{title}</h3>}
        </div>

        <div className="entity-list__controls">
          <input
            className="entity-list__search"
            placeholder={searchPlaceholder}
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
              setPage(1)
            }}
          />

          {sortableColumns.length > 0 && (
            <select
              className="entity-list__sort"
              value={sortField}
              onChange={(event) => {
                setSortField(event.target.value)
                setPage(1)
              }}
            >
              {sortableColumns.map((column) => (
                <option key={column.key} value={column.key}>
                  Sort by {column.header}
                </option>
              ))}
            </select>
          )}
        </div>
      </div>

      <div className="entity-list__table-wrap">
        <table className="entity-list__table">
          <thead>
            <tr>
              {columns.map((column) => (
                <th key={column.key}>{column.header}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {paginatedItems.map((item) => (
              <tr key={rowKey(item)}>
                {columns.map((column) => (
                  <td key={column.key}>{column.render(item)}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {loading && <div className="entity-list__state">Loading...</div>}
      {!loading && filteredItems.length === 0 && (
        <div className="entity-list__state">{emptyMessage}</div>
      )}

      {filteredItems.length > pageSize && (
        <div className="entity-list__pagination">
          <button
            type="button"
            onClick={() => setPage((current) => Math.max(1, current - 1))}
            disabled={currentPage === 1}
          >
            Previous
          </button>
          <span>
            Page {currentPage} of {totalPages}
          </span>
          <button
            type="button"
            onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
            disabled={currentPage === totalPages}
          >
            Next
          </button>
        </div>
      )}
    </div>
  )
}
