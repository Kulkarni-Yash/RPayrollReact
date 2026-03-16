import { useState } from 'react'
import FormBuilderUI from '../features/formBuilder/FormBuilderUI'
import FormRenderer from '../features/formBuilder/FormRenderer'
import type { FormLayoutConfig } from '../features/formBuilder/types'
import { createDefaultFormTab } from '../features/formBuilder/types'

import '../features/formBuilder/FormBuilderUI.css'

type FormBuilderPageProps = {
  view: 'create' | 'view'
  token: string
  editLayoutKey?: string | null
  onMenusChanged?: () => void
}

const emptyConfig: FormLayoutConfig = {
  name: 'Employee Form',
  entity: 'employee',
  entityLabel: 'Employee',
  gridSize: '3x4',
  layout: [],
  tabs: [createDefaultFormTab()],
}

export default function FormBuilderPage({ view, token, editLayoutKey, onMenusChanged }: FormBuilderPageProps) {
  const [config, setConfig] = useState<FormLayoutConfig>(emptyConfig)
  const [previewViewport, setPreviewViewport] = useState<'desktop' | 'mobile'>('desktop')

  const hasPreviewContent =
    config.tabs.some((tab) => tab.layout.length > 0) || config.layout.length > 0

  return (
    <div className="form-builder-page">
      <FormBuilderUI
        token={token}
        editLayoutKey={editLayoutKey}
        className={view === 'create' ? '' : 'form-builder--hidden'}
        onConfigChange={setConfig}
        onLayoutSaved={onMenusChanged}
      />

      {view === 'view' && (
        <div className="form-builder-preview-window">
          <div className="form-builder-preview-window__header">
            <div>
              <h3>{config.name ?? `${config.entity} Form`} Preview</h3>
              <p>Live preview of the current builder state.</p>
            </div>
            <div className="form-builder-preview-window__toolbar">
              <button
                type="button"
                className={previewViewport === 'desktop' ? 'active' : ''}
                onClick={() => setPreviewViewport('desktop')}
              >
                Desktop View
              </button>
              <button
                type="button"
                className={previewViewport === 'mobile' ? 'active' : ''}
                onClick={() => setPreviewViewport('mobile')}
              >
                Mobile View
              </button>
            </div>
          </div>

          {hasPreviewContent ? (
            <div className={`form-builder-preview-stage ${previewViewport}`}>
              <FormRenderer config={config} token={token} submitEnabled={false} />
            </div>
          ) : (
            <div className="form-builder__message error">
              Add controls in Create mode to preview the form here.
            </div>
          )}
        </div>
      )}
    </div>
  )
}
