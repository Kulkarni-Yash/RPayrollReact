import { useEffect, useMemo, useState } from 'react'
import type { DragEvent } from 'react'
import {
  canPlaceItem,
  generateCanvasItem,
  getCanvasRows,
  readDragPayload,
  startDrag,
} from './DragDropManager'
import { CONTROL_CATALOG } from './entities'
import { renderBuilderInput } from './FormRenderer'
import { bindVariable, canBindVariable, getUsedVariables, validateLayout } from './VariableBinder'
import { apiFetch, readErrorMessage } from '../../lib/api'
import type {
  EntityVariable,
  FormCanvasItem,
  FormEntityKey,
  FormLayoutConfig,
  FormLayoutTab,
  FormModelMetadata,
  FormModelSummary,
  RoleOption,
} from './types'
import { createDefaultFormTab, flattenTabLayouts, normalizeFormLayoutConfig } from './types'
import './FormBuilderUI.css'

type FormBuilderUIProps = {
  token: string
  editLayoutKey?: string | null
  className?: string
  onConfigChange?: (config: FormLayoutConfig) => void
  onLayoutSaved?: () => void
}

export default function FormBuilderUI({
  token,
  editLayoutKey,
  className,
  onConfigChange,
  onLayoutSaved,
}: FormBuilderUIProps) {
  const [models, setModels] = useState<FormModelSummary[]>([])
  const [entity, setEntity] = useState<FormEntityKey>('employee')
  const [modelMetadata, setModelMetadata] = useState<FormModelMetadata | null>(null)
  const [tabs, setTabs] = useState<FormLayoutTab[]>([createDefaultFormTab()])
  const [activeTabId, setActiveTabId] = useState<string>('')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [viewport, setViewport] = useState<'desktop' | 'mobile'>('desktop')
  const [leftCollapsed, setLeftCollapsed] = useState(false)
  const [rightCollapsed, setRightCollapsed] = useState(false)
  const [validationMessage, setValidationMessage] = useState('')
  const [saveMessage, setSaveMessage] = useState('')
  const [propertyMessage, setPropertyMessage] = useState('')
  const [draggedTabId, setDraggedTabId] = useState<string | null>(null)
  const [currentLayoutKey, setCurrentLayoutKey] = useState<string | null>(null)
  const [currentMenuKey, setCurrentMenuKey] = useState<string | null>(null)
  const [formName, setFormName] = useState('')
  const [saveMode, setSaveMode] = useState<'update' | 'draft'>('update')
  const [visibilityScope, setVisibilityScope] = useState<'allUsers' | 'roleBased' | 'ownerOnly'>('allUsers')
  const [maxHierarchyLevelAllowed, setMaxHierarchyLevelAllowed] = useState<number | undefined>(undefined)
  const [roles, setRoles] = useState<RoleOption[]>([])

  const variables = modelMetadata?.fields ?? []
  const allLayoutItems = useMemo(() => flattenTabLayouts(tabs), [tabs])
  const usedVariables = useMemo(() => getUsedVariables(allLayoutItems), [allLayoutItems])
  const validation = useMemo(() => validateLayout(allLayoutItems, variables), [allLayoutItems, variables])
  const isEditingExisting = Boolean(currentLayoutKey)
  const roleOptions = useMemo(
    () => [...roles].sort((left, right) => left.hierarchyLevel - right.hierarchyLevel),
    [roles]
  )

  const activeTab = useMemo(
    () => tabs.find((tab) => tab.id === activeTabId) ?? tabs[0] ?? createDefaultFormTab(),
    [activeTabId, tabs]
  )
  const activeLayout = activeTab.layout
  const selectedControl = activeLayout.find((item) => item.id === selectedId) ?? null
  const { builderColumns, builderRows } = useMemo(() => {
    const match = activeTab.gridSize.trim().match(/^(\d+)x(\d+)$/i)
    if (!match) {
      return { builderColumns: 3, builderRows: 4 }
    }

    return {
      builderColumns: Math.max(1, Number(match[1]) || 3),
      builderRows: Math.max(1, Number(match[2]) || 4),
    }
  }, [activeTab.gridSize])
  const canvasRows = getCanvasRows(activeLayout, builderRows)

  useEffect(() => {
    let active = true

    const loadModels = async () => {
      try {
        const response = await apiFetch('/api/form-metadata/models', {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        })

        if (!response.ok) {
          return
        }

        const data = (await response.json()) as FormModelSummary[]
        if (!active || !Array.isArray(data) || data.length === 0) {
          return
        }

        setModels(data)
        setEntity((current) =>
          data.some((item) => item.key === current) ? current : data[0].key
        )

        const rolesResponse = await apiFetch('/api/roles', {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        })

        if (rolesResponse.ok && active) {
          const roleData = (await rolesResponse.json()) as RoleOption[]
          setRoles(Array.isArray(roleData) ? roleData : [])
        }
      } catch {
        // keep defaults empty
      }
    }

    loadModels()

    return () => {
      active = false
    }
  }, [token])

  useEffect(() => {
    let active = true

    const loadExplicitLayout = async () => {
      try {
        const response = await apiFetch(`/api/form-layouts/${editLayoutKey}`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        })

        if (!response.ok) {
          return
        }

        const existing = normalizeFormLayoutConfig((await response.json()) as FormLayoutConfig)
        const metadataResponse = await apiFetch(`/api/form-metadata/models/${existing.entity}`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        })

        if (!metadataResponse.ok || !active) {
          return
        }

        const metadata = (await metadataResponse.json()) as FormModelMetadata
        const nextTabs = existing.tabs.length > 0 ? existing.tabs : [createDefaultFormTab()]

        setEntity(existing.entity)
        setModelMetadata(metadata)
        setTabs(nextTabs)
        setActiveTabId(nextTabs[0].id)
        setCurrentLayoutKey(existing.key ?? editLayoutKey ?? null)
        setCurrentMenuKey(existing.menuKey ?? null)
        setFormName(existing.name ?? `${metadata.label} Form`)
        setSaveMode('update')
        setVisibilityScope(existing.visibilityScope ?? 'allUsers')
        setMaxHierarchyLevelAllowed(existing.maxHierarchyLevelAllowed)
        setSelectedId(null)
        setValidationMessage('')
        setSaveMessage('')
        setPropertyMessage('')
      } catch {
        // ignore
      }
    }

    if (editLayoutKey) {
      void loadExplicitLayout()
    }

    return () => {
      active = false
    }
  }, [editLayoutKey, token])

  useEffect(() => {
    let active = true

    const loadMetadata = async () => {
      if (editLayoutKey) {
        return
      }

      try {
        const response = await apiFetch(`/api/form-metadata/models/${entity}`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        })

        if (!response.ok) {
          return
        }

        const data = (await response.json()) as FormModelMetadata
        if (!active) {
          return
        }

        setModelMetadata(data)

        const existingResponse = await apiFetch(`/api/form-layouts/model/${entity}`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        })

        if (existingResponse.ok) {
          const existing = normalizeFormLayoutConfig((await existingResponse.json()) as FormLayoutConfig)
          const nextTabs = existing.tabs.length > 0 ? existing.tabs : [createDefaultFormTab()]
          setTabs(nextTabs)
          setActiveTabId(nextTabs[0].id)
          setCurrentLayoutKey(existing.key ?? null)
          setCurrentMenuKey(existing.menuKey ?? null)
          setFormName(existing.name ?? `${data.label} Form`)
          setSaveMode('update')
          setVisibilityScope(existing.visibilityScope ?? 'allUsers')
          setMaxHierarchyLevelAllowed(existing.maxHierarchyLevelAllowed)
        } else {
          const defaultTab = createDefaultFormTab()
          setTabs([defaultTab])
          setActiveTabId(defaultTab.id)
          setCurrentLayoutKey(null)
          setCurrentMenuKey(null)
          setFormName(`${data.label} Form`)
          setSaveMode('update')
          setVisibilityScope('allUsers')
          setMaxHierarchyLevelAllowed(undefined)
        }

        setSelectedId(null)
        setValidationMessage('')
        setSaveMessage('')
        setPropertyMessage('')
      } catch {
        // ignore
      }
    }

    if (entity) {
      loadMetadata()
    }

    return () => {
      active = false
    }
  }, [editLayoutKey, entity, token])

  const formConfig: FormLayoutConfig = useMemo(
    () => ({
      key: currentLayoutKey ?? undefined,
      menuKey: currentMenuKey ?? undefined,
      name: formName,
      targetLayoutKey: currentLayoutKey ?? undefined,
      saveMode,
      visibilityScope,
      maxHierarchyLevelAllowed: visibilityScope === 'roleBased' ? maxHierarchyLevelAllowed : undefined,
      entity,
      entityLabel: modelMetadata?.label,
      gridSize: activeTab.gridSize,
      submitUrl: modelMetadata?.submitUrl,
      submitMethod: modelMetadata?.submitMethod,
      layout: [...activeLayout].sort((left, right) => {
        if (left.row === right.row) return left.column - right.column
        return left.row - right.row
      }),
      tabs: tabs.map((tab) => ({
        ...tab,
        layout: [...tab.layout].sort((left, right) => {
          if (left.row === right.row) return left.column - right.column
          return left.row - right.row
        }),
      })),
    }),
    [activeLayout, activeTab.gridSize, currentLayoutKey, currentMenuKey, entity, formName, maxHierarchyLevelAllowed, modelMetadata, saveMode, tabs, visibilityScope]
  )

  useEffect(() => {
    onConfigChange?.(formConfig)
  }, [formConfig, onConfigChange])

  const updateActiveTab = (updater: (tab: FormLayoutTab) => FormLayoutTab) => {
    setTabs((current) =>
      current.map((tab) => (tab.id === activeTab.id ? updater(tab) : tab))
    )
  }

  const handleDropOnCell = (event: DragEvent, row: number, column: number) => {
    event.preventDefault()
    const payload = readDragPayload(event)
    if (!payload) return

    if (payload.source === 'control') {
      if (!canPlaceItem(activeLayout, row, column, 1, builderColumns)) {
        setPropertyMessage('That grid slot is already occupied.')
        return
      }

      const nextItem = generateCanvasItem(payload.controlType, row, column)
      updateActiveTab((tab) => ({ ...tab, layout: [...tab.layout, nextItem] }))
      setSelectedId(nextItem.id)
      setPropertyMessage('')
      return
    }

    if (payload.source === 'variable' && selectedId) {
      const variable = variables.find((item) => item.key === payload.variableKey)
      if (!variable || !canBindVariable(allLayoutItems, variable.key, selectedId)) {
        setPropertyMessage('That variable is already used in this form.')
        return
      }
      updateActiveTab((tab) => ({
        ...tab,
        layout: bindVariable(tab.layout, selectedId, variable),
      }))
      setPropertyMessage('')
    }
  }

  const bindVariableToControl = (controlId: string, variable: EntityVariable) => {
    if (!canBindVariable(allLayoutItems, variable.key, controlId)) {
      setPropertyMessage('That variable is already mapped.')
      return
    }

    setSelectedId(controlId)
    updateActiveTab((tab) => ({
      ...tab,
      layout: bindVariable(tab.layout, controlId, variable),
    }))
    setPropertyMessage('')
  }

  const handleBindVariable = (variable: EntityVariable) => {
    if (!selectedId) {
      setPropertyMessage('Select a control on the canvas before binding a variable.')
      return
    }

    bindVariableToControl(selectedId, variable)
  }

  const updateSelectedControl = (updater: (item: FormCanvasItem) => FormCanvasItem) => {
    if (!selectedId) return

    updateActiveTab((tab) => ({
      ...tab,
      layout: tab.layout.map((item) => {
        if (item.id !== selectedId) {
          return item
        }

        const next = updater(item)
        if (!canPlaceItem(tab.layout, next.row, next.column, next.width, builderColumns, next.id)) {
          setPropertyMessage('Width change overlaps another control in this row.')
          return item
        }

        return next
      }),
    }))
  }

  const handleSave = () => {
    if (!modelMetadata?.submitUrl || !modelMetadata.submitMethod) {
      setValidationMessage('Model metadata is incomplete.')
      return
    }

    if (!formName.trim()) {
      setValidationMessage('Form name is required.')
      setSaveMessage('')
      return
    }

    if (!validation.valid) {
      const missing = validation.missingRequired.map((item) => item.label).join(', ')
      setValidationMessage(`Missing required variables: ${missing}`)
      setSaveMessage('')
      return
    }

    void (async () => {
      const payload = {
        ...formConfig,
        visibilityScope:
          visibilityScope === 'roleBased'
            ? 1
            : visibilityScope === 'ownerOnly'
              ? 2
              : 0,
      }

      const response = await apiFetch('/api/form-layouts', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(payload),
      })

      if (!response.ok) {
        setValidationMessage(await readErrorMessage(response))
        setSaveMessage('')
        return
      }

      const saved = normalizeFormLayoutConfig((await response.json()) as FormLayoutConfig)
      setTabs(saved.tabs.length > 0 ? saved.tabs : tabs)
      setActiveTabId((current) =>
        saved.tabs.some((tab) => tab.id === current) ? current : (saved.tabs[0]?.id ?? current)
      )
      setCurrentLayoutKey(saved.key ?? null)
      setCurrentMenuKey(saved.menuKey ?? null)
      setFormName(saved.name ?? formName)
      setSaveMode('update')
      setValidationMessage('')
      setSaveMessage(
        saveMode === 'draft'
          ? `${saved.name ?? modelMetadata.label} draft saved. Menu item created as inactive.`
          : `${saved.name ?? modelMetadata.label} updated successfully.`
      )
      onLayoutSaved?.()
    })()
  }

  const startNewForm = () => {
    const defaultTab = createDefaultFormTab()
    setTabs([defaultTab])
    setActiveTabId(defaultTab.id)
    setCurrentLayoutKey(null)
    setCurrentMenuKey(null)
    setFormName(`${modelMetadata?.label ?? entity} Form`)
    setSaveMode('draft')
    setVisibilityScope('allUsers')
    setMaxHierarchyLevelAllowed(undefined)
    setSelectedId(null)
    setValidationMessage('')
    setSaveMessage('')
    setPropertyMessage('')
  }

  const removeSelectedControl = () => {
    if (!selectedId) return
    updateActiveTab((tab) => ({
      ...tab,
      layout: tab.layout.filter((item) => item.id !== selectedId),
    }))
    setSelectedId(null)
    setPropertyMessage('')
  }

  const addTab = () => {
    const nextTab = createDefaultFormTab(`Tab ${tabs.length + 1}`)
    setTabs((current) => [...current, nextTab])
    setActiveTabId(nextTab.id)
    setSelectedId(null)
    setPropertyMessage('')
  }

  const duplicateActiveTab = () => {
    const duplicatedTab: FormLayoutTab = {
      ...activeTab,
      id: `tab-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      name: `${activeTab.name} Copy`,
      layout: activeTab.layout.map((item) => ({
        ...item,
        id: `${item.id}-copy-${Math.random().toString(36).slice(2, 8)}`,
      })),
    }

    const activeIndex = tabs.findIndex((tab) => tab.id === activeTab.id)
    const nextTabs = [...tabs]
    nextTabs.splice(activeIndex + 1, 0, duplicatedTab)
    setTabs(nextTabs)
    setActiveTabId(duplicatedTab.id)
    setSelectedId(null)
    setPropertyMessage('')
  }

  const removeTab = (tabId: string) => {
    if (tabs.length === 1) {
      setPropertyMessage('At least one tab is required.')
      return
    }

    const nextTabs = tabs.filter((tab) => tab.id !== tabId)
    setTabs(nextTabs)
    if (activeTabId === tabId) {
      setActiveTabId(nextTabs[0].id)
    }
    setSelectedId(null)
    setPropertyMessage('')
  }

  const updateActiveTabName = (name: string) => {
    updateActiveTab((tab) => ({ ...tab, name }))
  }

  const updateActiveTabGridSize = (gridSize: string) => {
    updateActiveTab((tab) => ({ ...tab, gridSize }))
  }

  const switchTab = (tabId: string) => {
    setActiveTabId(tabId)
    setSelectedId(null)
    setPropertyMessage('')
  }

  const reorderTabs = (sourceTabId: string, targetTabId: string) => {
    if (sourceTabId === targetTabId) {
      return
    }

    setTabs((current) => {
      const sourceIndex = current.findIndex((tab) => tab.id === sourceTabId)
      const targetIndex = current.findIndex((tab) => tab.id === targetTabId)

      if (sourceIndex === -1 || targetIndex === -1) {
        return current
      }

      const next = [...current]
      const [moved] = next.splice(sourceIndex, 1)
      next.splice(targetIndex, 0, moved)
      return next
    })
  }

  const renderCanvasGrid = () => {
    const cells: Array<{ row: number; column: number }> = []
    for (let row = 1; row <= canvasRows; row += 1) {
      for (let column = 1; column <= builderColumns; column += 1) {
        cells.push({ row, column })
      }
    }

    const cellNodes = cells.map((cell) => (
      <div
        key={`${cell.row}-${cell.column}`}
        className="form-builder__canvas-cell"
        style={{ gridColumn: cell.column, gridRow: cell.row }}
        onDragOver={(event) => event.preventDefault()}
        onDrop={(event) => handleDropOnCell(event, cell.row, cell.column)}
      >
        <div className="form-builder__drop-hint">Drop Here</div>
      </div>
    ))

    const itemNodes = activeLayout.map((item) => (
      <button
        key={item.id}
        type="button"
        className={`form-builder__placed-control ${selectedId === item.id ? 'selected' : ''}`}
        style={{
          gridColumn: `${item.column} / span ${Math.min(item.width, builderColumns)}`,
          gridRow: item.row,
        }}
        onClick={() => {
          setSelectedId(item.id)
          setPropertyMessage('')
        }}
        onDragOver={(event) => event.preventDefault()}
        onDrop={(event: DragEvent) => {
          event.preventDefault()
          const payload = readDragPayload(event)
          if (!payload || payload.source !== 'variable') return
          const variable = variables.find((entry) => entry.key === payload.variableKey)
          if (!variable) return
          bindVariableToControl(item.id, variable)
        }}
      >
        <span className="form-builder__placed-label">{item.label || 'Untitled'}</span>
        <span className="form-builder__placed-preview">{renderBuilderInput(item)}</span>
        <span className="form-builder__placed-meta">
          {item.type} {item.variable ? `• ${item.variable}` : '• Unbound'}
        </span>
      </button>
    ))

    return (
      <>
        {cellNodes}
        {itemNodes}
      </>
    )
  }

  return (
    <div className={`form-builder ${className ?? ''}`.trim()}>
      <div className="form-builder__toolbar">
        <div className="form-builder__toolbar-left">
          <select
            value={entity}
            disabled={isEditingExisting}
            onChange={(event) => setEntity(event.target.value as FormEntityKey)}
          >
            {models.map((item) => (
              <option key={item.key} value={item.key}>
                {item.label}
              </option>
            ))}
          </select>
          <input
            className="form-builder__name-input"
            value={formName}
            onChange={(event) => setFormName(event.target.value)}
            placeholder="Form name"
            aria-label="Form name"
          />
          <button type="button" onClick={() => setViewport('desktop')} className={viewport === 'desktop' ? 'active' : ''}>
            Desktop View
          </button>
          <button type="button" onClick={() => setViewport('mobile')} className={viewport === 'mobile' ? 'active' : ''}>
            Mobile View
          </button>
          {isEditingExisting && (
            <button type="button" onClick={startNewForm}>
              New Form
            </button>
          )}
        </div>

        <div className="form-builder__toolbar-right">
          <select
            value={visibilityScope}
            onChange={(event) =>
              setVisibilityScope(event.target.value as 'allUsers' | 'roleBased' | 'ownerOnly')
            }
          >
            <option value="allUsers">Visible To Everyone</option>
            <option value="roleBased">Visible By Role</option>
            <option value="ownerOnly">Only Me</option>
          </select>
          {visibilityScope === 'roleBased' && (
            <select
              value={maxHierarchyLevelAllowed ?? ''}
              onChange={(event) =>
                setMaxHierarchyLevelAllowed(
                  event.target.value ? Number(event.target.value) : undefined
                )
              }
            >
              <option value="">Choose max hierarchy</option>
              {roleOptions.map((role) => (
                <option key={role.id} value={role.hierarchyLevel}>
                  {role.name} and higher
                </option>
              ))}
            </select>
          )}
          {(isEditingExisting || saveMode === 'draft') && (
            <select value={saveMode} onChange={(event) => setSaveMode(event.target.value as 'update' | 'draft')}>
              <option value="update">Update Existing</option>
              <option value="draft">Save As Draft</option>
            </select>
          )}
          <button type="button" onClick={handleSave}>Save Layout</button>
        </div>
      </div>

      {validationMessage && <div className="form-builder__message error">{validationMessage}</div>}
      {saveMessage && <div className="form-builder__message success">{saveMessage}</div>}

      <div className="form-builder__layout">
        <aside className={`form-builder__panel left ${leftCollapsed ? 'collapsed' : ''}`}>
          <div className="form-builder__panel-header">
            <div>
              <h3>Variables</h3>
              <p>Entity: {entity}</p>
            </div>
            <button type="button" onClick={() => setLeftCollapsed((current) => !current)}>
              {leftCollapsed ? '→' : '←'}
            </button>
          </div>

          {!leftCollapsed && (
            <div className="form-builder__panel-body">
              {variables.map((variable) => {
                const isUsed = usedVariables.has(variable.key) && selectedControl?.variable !== variable.key
                return (
                  <button
                    key={variable.key}
                    type="button"
                    className={`form-builder__variable ${isUsed ? 'used' : ''}`}
                    draggable={!isUsed}
                    disabled={isUsed}
                    onDragStart={(event) => startDrag(event, { source: 'variable', variableKey: variable.key })}
                    onClick={() => handleBindVariable(variable)}
                  >
                    <span>{variable.label}</span>
                    <small>{variable.key}{variable.required ? ' • required' : ''}</small>
                  </button>
                )
              })}
            </div>
          )}
        </aside>

        <section className="form-builder__workspace">
          <div className="form-builder__workspace-header">
            <div>
              <h3>Form Canvas</h3>
              <p>
                {isEditingExisting
                  ? `Editing ${formName || modelMetadata?.label || entity}. Model and submit API are locked.`
                  : 'Tabs help us split larger forms without endless scrolling.'}
              </p>
            </div>
            <div className="form-builder__workspace-status">
              {validation.allMapped ? 'All variables mapped' : `${usedVariables.size}/${variables.length} mapped`}
            </div>
          </div>

          <div className="form-builder__tabs-nav">
            <div className="form-builder__tabs-list">
              {tabs.map((tab) => (
                <button
                  key={tab.id}
                  type="button"
                  className={`form-builder__tab ${tab.id === activeTab.id ? 'active' : ''}`}
                  draggable
                  onClick={() => switchTab(tab.id)}
                  onDragStart={() => setDraggedTabId(tab.id)}
                  onDragEnd={() => setDraggedTabId(null)}
                  onDragOver={(event) => event.preventDefault()}
                  onDrop={() => {
                    if (draggedTabId) {
                      reorderTabs(draggedTabId, tab.id)
                    }
                    setDraggedTabId(null)
                  }}
                >
                  {tab.name}
                </button>
              ))}
              <button type="button" className="form-builder__tab-add" onClick={addTab}>
                + Add Tab
              </button>
            </div>
          </div>

          <div className="form-builder__tab-tools">
            <input
              className="form-builder__tab-name-input"
              value={activeTab.name}
              onChange={(event) => updateActiveTabName(event.target.value)}
              placeholder="Tab name"
              aria-label="Tab name"
            />
            <input
              className="form-builder__grid-input"
              value={activeTab.gridSize}
              onChange={(event) => updateActiveTabGridSize(event.target.value)}
              placeholder="3x4"
              aria-label="Grid size"
            />
            <button type="button" onClick={() => removeTab(activeTab.id)} disabled={tabs.length === 1}>
              Remove Tab
            </button>
            <button type="button" onClick={duplicateActiveTab}>
              Duplicate Tab
            </button>
          </div>

          <div className={`form-builder__canvas-shell ${viewport}`}>
            <div
              className="form-builder__canvas-grid"
              style={{
                gridTemplateColumns: `repeat(${builderColumns}, minmax(0, 1fr))`,
                gridTemplateRows: `repeat(${canvasRows}, minmax(88px, auto))`,
                maxWidth: `${builderColumns * 240 + (builderColumns - 1) * 14}px`,
              }}
            >
              {renderCanvasGrid()}
            </div>
          </div>

          <div className="form-builder__json-block">
            <h4>Layout JSON</h4>
            <pre>{JSON.stringify(formConfig, null, 2)}</pre>
          </div>
        </section>

        <aside className={`form-builder__panel right ${rightCollapsed ? 'collapsed' : ''}`}>
          <div className="form-builder__panel-header">
            <div>
              <h3>Input Controls</h3>
              <p>Drag onto the canvas</p>
            </div>
            <button type="button" onClick={() => setRightCollapsed((current) => !current)}>
              {rightCollapsed ? '←' : '→'}
            </button>
          </div>

          {!rightCollapsed && (
            <div className="form-builder__panel-body">
              <div className="form-builder__control-list">
                {CONTROL_CATALOG.map((control) => (
                  <button
                    key={control.type}
                    type="button"
                    className="form-builder__control-tile"
                    draggable
                    onDragStart={(event) => startDrag(event, { source: 'control', controlType: control.type })}
                  >
                    <strong>{control.label}</strong>
                    <span>{control.hint}</span>
                  </button>
                ))}
              </div>

              <div className="form-builder__properties">
                <div className="form-builder__properties-header">
                  <h4>Selected Control</h4>
                  {selectedControl && (
                    <button type="button" onClick={removeSelectedControl}>
                      Remove
                    </button>
                  )}
                </div>

                {!selectedControl ? (
                  <p className="form-builder__empty-state">Select a control on the canvas to edit its properties.</p>
                ) : (
                  <div className="form-builder__properties-form">
                    <label>
                      Label
                      <input
                        value={selectedControl.label}
                        onChange={(event) => updateSelectedControl((item) => ({ ...item, label: event.target.value }))}
                      />
                    </label>

                    <label>
                      Placeholder
                      <input
                        value={selectedControl.placeholder}
                        onChange={(event) => updateSelectedControl((item) => ({ ...item, placeholder: event.target.value }))}
                      />
                    </label>

                    <label>
                      Default Value
                      <input
                        value={selectedControl.defaultValue}
                        onChange={(event) => updateSelectedControl((item) => ({ ...item, defaultValue: event.target.value }))}
                      />
                    </label>

                    <label className="inline">
                      <input
                        type="checkbox"
                        checked={selectedControl.required}
                        onChange={(event) => updateSelectedControl((item) => ({ ...item, required: event.target.checked }))}
                      />
                      Required
                    </label>

                    <label>
                      Width
                      <select
                        value={selectedControl.width}
                        onChange={(event) =>
                          updateSelectedControl((item) => ({
                            ...item,
                            width: Number(event.target.value),
                          }))
                        }
                      >
                        <option value={1}>1 column</option>
                        {builderColumns >= 2 && <option value={2}>2 columns</option>}
                        {builderColumns >= 3 && <option value={builderColumns}>Full row</option>}
                      </select>
                    </label>

                    <label>
                      Variable Binding
                      <select
                        value={selectedControl.variable ?? ''}
                        onChange={(event) => {
                          const variable = variables.find((item) => item.key === event.target.value)
                          if (!variable) return
                          handleBindVariable(variable)
                        }}
                      >
                        <option value="">Select variable...</option>
                        {variables.map((variable) => (
                          <option
                            key={variable.key}
                            value={variable.key}
                            disabled={!canBindVariable(allLayoutItems, variable.key, selectedControl.id)}
                          >
                            {variable.label}
                          </option>
                        ))}
                      </select>
                    </label>
                  </div>
                )}

                {propertyMessage && <div className="form-builder__inline-note">{propertyMessage}</div>}
              </div>
            </div>
          )}
        </aside>
      </div>
    </div>
  )
}
