export type FormEntityKey = string

export type FormControlType =
  | 'textbox'
  | 'numeric'
  | 'dropdown'
  | 'dropdownSearch'
  | 'date'
  | 'dateTime'
  | 'checkbox'
  | 'textArea'

export type EntityVariable = {
  key: string
  label: string
  dataType: 'string' | 'number' | 'date' | 'datetime' | 'boolean' | 'lookup'
  required?: boolean
  nullable?: boolean
}

export type FormModelSummary = {
  key: string
  label: string
  dtoName: string
  submitUrl: string
  submitMethod: string
}

export type FormModelMetadata = FormModelSummary & {
  fields: EntityVariable[]
}

export type FormCanvasItem = {
  id: string
  type: FormControlType
  variable?: string
  label: string
  placeholder: string
  required: boolean
  defaultValue: string
  width: number
  row: number
  column: number
}

export type FormLayoutTab = {
  id: string
  name: string
  gridSize: string
  layout: FormCanvasItem[]
}

export type FormLayoutConfig = {
  key?: string
  menuKey?: string
  name?: string
  targetLayoutKey?: string
  saveMode?: 'update' | 'draft'
  ownerUserId?: number
  visibilityScope?: 'allUsers' | 'roleBased' | 'ownerOnly'
  maxHierarchyLevelAllowed?: number
  entity: FormEntityKey
  entityLabel?: string
  gridSize: string
  submitUrl?: string
  submitMethod?: string
  layout: FormCanvasItem[]
  tabs: FormLayoutTab[]
}

export type DragPayload =
  | { source: 'control'; controlType: FormControlType }
  | { source: 'variable'; variableKey: string }

export type RoleOption = {
  id: number
  name: string
  hierarchyLevel: number
}

export function createDefaultFormTab(name = 'Tab 1'): FormLayoutTab {
  return {
    id: `tab-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    name,
    gridSize: '3x4',
    layout: [],
  }
}

export function normalizeFormLayoutConfig(
  config: Partial<FormLayoutConfig> | null | undefined
): FormLayoutConfig {
  const legacyGridSize = typeof config?.gridSize === 'string' && config.gridSize ? config.gridSize : '3x4'
  const legacyLayout = Array.isArray(config?.layout) ? config.layout : []
  const rawVisibilityScope = config?.visibilityScope as string | number | undefined

  const tabs =
    Array.isArray(config?.tabs) && config.tabs.length > 0
      ? config.tabs.map((tab, index) => ({
          id: tab.id || `tab-${index + 1}`,
          name: tab.name || `Tab ${index + 1}`,
          gridSize: tab.gridSize || legacyGridSize,
          layout: Array.isArray(tab.layout) ? tab.layout : [],
        }))
      : [
          {
            id: 'tab-1',
            name: 'Tab 1',
            gridSize: legacyGridSize,
            layout: legacyLayout,
          },
        ]

  const normalizedVisibilityScope =
    rawVisibilityScope === 'roleBased' || rawVisibilityScope === 1
      ? 'roleBased'
      : rawVisibilityScope === 'ownerOnly' || rawVisibilityScope === 2
        ? 'ownerOnly'
        : 'allUsers'

  return {
    key: config?.key,
    menuKey: config?.menuKey,
    name: config?.name,
    targetLayoutKey: config?.targetLayoutKey,
    saveMode:
      config?.saveMode === 'draft' || config?.saveMode === 'update'
        ? config.saveMode
        : 'update',
    ownerUserId: config?.ownerUserId,
    visibilityScope: normalizedVisibilityScope,
    maxHierarchyLevelAllowed: config?.maxHierarchyLevelAllowed,
    entity: config?.entity ?? 'employee',
    entityLabel: config?.entityLabel,
    gridSize: legacyGridSize,
    submitUrl: config?.submitUrl,
    submitMethod: config?.submitMethod,
    layout: legacyLayout,
    tabs,
  }
}

export function flattenTabLayouts(tabs: FormLayoutTab[]) {
  return tabs.flatMap((tab) => tab.layout)
}
