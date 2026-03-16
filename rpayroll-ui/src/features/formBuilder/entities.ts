import type { FormControlType } from './types'

export const CONTROL_CATALOG: Array<{ type: FormControlType; label: string; hint: string }> = [
  { type: 'textbox', label: 'Textbox', hint: 'Short text input' },
  { type: 'numeric', label: 'Numeric', hint: 'Numbers only' },
  { type: 'dropdown', label: 'Dropdown', hint: 'Static select list' },
  { type: 'dropdownSearch', label: 'DropdownSearch', hint: 'Searchable select' },
  { type: 'date', label: 'Date', hint: 'Calendar date' },
  { type: 'dateTime', label: 'DateTime', hint: 'Date and time' },
  { type: 'checkbox', label: 'Checkbox', hint: 'Boolean toggle' },
  { type: 'textArea', label: 'TextArea', hint: 'Multi-line text' }
]
