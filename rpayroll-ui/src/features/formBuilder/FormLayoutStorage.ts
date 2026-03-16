import type { FormEntityKey, FormLayoutConfig } from './types'
import { normalizeFormLayoutConfig } from './types'

const STORAGE_PREFIX = 'rpayroll_form_builder_'

export function getStorageKey(entity: FormEntityKey) {
  return `${STORAGE_PREFIX}${entity.toLowerCase()}`
}

export function saveFormLayout(config: FormLayoutConfig) {
  localStorage.setItem(getStorageKey(config.entity), JSON.stringify(config))
}

export function loadFormLayout(entity: FormEntityKey): FormLayoutConfig | null {
  try {
    const raw = localStorage.getItem(getStorageKey(entity))
    if (!raw) {
      return null
    }
    const parsed = JSON.parse(raw) as Partial<FormLayoutConfig>
    if (!parsed) {
      return null
    }

    return normalizeFormLayoutConfig({ ...parsed, entity })
  } catch {
    return null
  }
}
