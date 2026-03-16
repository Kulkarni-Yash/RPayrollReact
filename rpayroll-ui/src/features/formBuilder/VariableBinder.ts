import type { EntityVariable, FormCanvasItem } from './types'

export function getUsedVariables(layout: FormCanvasItem[]) {
  return new Set(layout.map((item) => item.variable).filter(Boolean) as string[])
}

export function canBindVariable(layout: FormCanvasItem[], variableKey: string, selectedId?: string) {
  return !layout.some((item) => item.variable === variableKey && item.id !== selectedId)
}

export function bindVariable(
  layout: FormCanvasItem[],
  selectedId: string,
  variable: EntityVariable
): FormCanvasItem[] {
  if (!canBindVariable(layout, variable.key, selectedId)) {
    return layout
  }

  return layout.map((item) =>
    item.id === selectedId
      ? {
          ...item,
          variable: variable.key,
          label: item.label || variable.label,
          required: variable.required ?? item.required
        }
      : item
  )
}

export function validateLayout(layout: FormCanvasItem[], variables: EntityVariable[]) {
  const usedVariables = getUsedVariables(layout)
  const missingRequired = variables.filter((variable) => variable.required && !usedVariables.has(variable.key))
  const allMapped = variables.every((variable) => usedVariables.has(variable.key))
  const valid = missingRequired.length === 0 || allMapped

  return {
    valid,
    missingRequired,
    allMapped,
    usedVariables
  }
}
