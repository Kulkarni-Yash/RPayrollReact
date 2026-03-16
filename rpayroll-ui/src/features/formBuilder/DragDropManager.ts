import type { DragEvent } from 'react'
import type { DragPayload, FormCanvasItem, FormControlType } from './types'

const DRAG_KEY = 'application/x-rpayroll-form-builder'

export function serializeDragPayload(payload: DragPayload) {
  return JSON.stringify(payload)
}

export function startDrag(event: DragEvent, payload: DragPayload) {
  event.dataTransfer.setData(DRAG_KEY, serializeDragPayload(payload))
  event.dataTransfer.effectAllowed = 'copyMove'
}

export function readDragPayload(event: DragEvent): DragPayload | null {
  const raw = event.dataTransfer.getData(DRAG_KEY)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as DragPayload
  } catch {
    return null
  }
}

export function generateCanvasItem(type: FormControlType, row: number, column: number): FormCanvasItem {
  const seed = `${type}-${row}-${column}-${Date.now()}`

  return {
    id: seed,
    type,
    label: prettyControlLabel(type),
    placeholder: '',
    required: false,
    defaultValue: '',
    width: 1,
    row,
    column
  }
}

export function canPlaceItem(
  layout: FormCanvasItem[],
  row: number,
  column: number,
  width: number,
  totalColumns = 3,
  ignoreId?: string
) {
  if (column + width - 1 > totalColumns) {
    return false
  }

  return !layout.some((item) => {
    if (item.id === ignoreId || item.row !== row) {
      return false
    }

    const itemStart = item.column
    const itemEnd = item.column + item.width - 1
    const nextStart = column
    const nextEnd = column + width - 1

    return nextStart <= itemEnd && nextEnd >= itemStart
  })
}

export function getCanvasRows(layout: FormCanvasItem[], baseRows = 4) {
  const highestRow = layout.reduce((max, item) => Math.max(max, item.row), 0)
  return Math.max(baseRows, highestRow + 1)
}

function prettyControlLabel(type: FormControlType) {
  switch (type) {
    case 'dropdownSearch':
      return 'Dropdown Search'
    case 'textArea':
      return 'Text Area'
    case 'dateTime':
      return 'Date Time'
    default:
      return type.charAt(0).toUpperCase() + type.slice(1)
  }
}
