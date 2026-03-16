import { useMemo, useRef, useState } from 'react'
import { apiFetch, readErrorMessage } from '../lib/api'
import './AIClassBuilderPage.css'

type AIClassBuilderPageProps = {
  username: string
  token: string
}

type ChatMessage = {
  id: string
  role: 'user' | 'assistant'
  content: string
}

const starterMessages: ChatMessage[] = [
  {
    id: 'assistant-welcome',
    role: 'assistant',
    content:
      "Tell me what class or backend structure you want to build. I'm connected through the API and can respond with this project's architecture in mind.",
  },
]

export default function AIClassBuilderPage({ username, token }: AIClassBuilderPageProps) {
  const [messages, setMessages] = useState<ChatMessage[]>(starterMessages)
  const [prompt, setPrompt] = useState('')
  const [sending, setSending] = useState(false)
  const [error, setError] = useState('')
  const [modelName, setModelName] = useState('deepseek-coder:6.7b')
  const [usedFiles, setUsedFiles] = useState<string[]>([])
  const scrollAnchorRef = useRef<HTMLDivElement | null>(null)

  const canSend = prompt.trim().length > 0 && !sending

  const examplePrompts = useMemo(
    () => [
      'Create a C# entity and DTO for project milestones with audit fields.',
      'Generate a repository interface and service contract for attendance approvals.',
      'Draft an API request/response DTO pair for payroll run submission.',
    ],
    []
  )

  const scrollToBottom = () => {
    requestAnimationFrame(() => {
      scrollAnchorRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' })
    })
  }

  const handleSend = async () => {
    const trimmedPrompt = prompt.trim()
    if (!trimmedPrompt || sending) {
      return
    }

    const userMessage: ChatMessage = {
      id: `user-${Date.now()}`,
      role: 'user',
      content: trimmedPrompt,
    }

    const nextMessages = [...messages, userMessage]
    setMessages(nextMessages)
    setPrompt('')
    setSending(true)
    setError('')
    scrollToBottom()

    try {
      const response = await apiFetch('/api/ai-class-builder/chat', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          messages: nextMessages.map((message) => ({
            role: message.role,
            content: message.content,
          })),
        }),
      })

      if (!response.ok) {
        throw new Error(await readErrorMessage(response))
      }

      const data = (await response.json()) as {
        content?: string
        model?: string
        usedFiles?: string[]
      }

      const assistantMessage: ChatMessage = {
        id: `assistant-${Date.now()}`,
        role: 'assistant',
        content: data.content?.trim() || 'I could not generate a response.',
      }

      setModelName(data.model?.trim() || 'deepseek-coder:6.7b')
      setUsedFiles(Array.isArray(data.usedFiles) ? data.usedFiles : [])
      setMessages((current) => [...current, assistantMessage])
      scrollToBottom()
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : 'Failed to reach the project-aware AI service.'
      )
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="ai-class-builder">
      <aside className="ai-class-builder__sidebar">
        <div className="ai-class-builder__card">
          <p className="ai-class-builder__eyebrow">Project-Aware Model</p>
          <h3>{modelName}</h3>
          <p>
            Connected through <code>RPayroll.API</code> with local Ollama and repo context.
          </p>
        </div>

        <div className="ai-class-builder__card">
          <p className="ai-class-builder__eyebrow">Context Files</p>
          <div className="ai-class-builder__context-list">
            {usedFiles.length > 0 ? (
              usedFiles.map((file) => <span key={file}>{file}</span>)
            ) : (
              <p>No specific files used yet.</p>
            )}
          </div>
        </div>

        <div className="ai-class-builder__card">
          <p className="ai-class-builder__eyebrow">Prompt Ideas</p>
          <div className="ai-class-builder__examples">
            {examplePrompts.map((example) => (
              <button
                key={example}
                type="button"
                onClick={() => setPrompt(example)}
              >
                {example}
              </button>
            ))}
          </div>
        </div>
      </aside>

      <section className="ai-class-builder__chat-shell">
        <div className="ai-class-builder__chat-header">
          <div>
            <p className="ai-class-builder__eyebrow">AI Class Builder</p>
            <h2>Build classes with chat</h2>
          </div>
          <div className="ai-class-builder__meta">
            <span>{username}</span>
            <span>Chat mode</span>
          </div>
        </div>

        <div className="ai-class-builder__messages">
          {messages.map((message) => (
            <div
              key={message.id}
              className={`ai-class-builder__message ${message.role}`}
            >
              <div className="ai-class-builder__message-role">
                {message.role === 'assistant' ? 'AI' : 'You'}
              </div>
              <div className="ai-class-builder__message-body">{message.content}</div>
            </div>
          ))}

          {sending && (
            <div className="ai-class-builder__message assistant">
              <div className="ai-class-builder__message-role">AI</div>
              <div className="ai-class-builder__message-body">Thinking...</div>
            </div>
          )}

          <div ref={scrollAnchorRef} />
        </div>

        <div className="ai-class-builder__composer">
          {error && <div className="ai-class-builder__error">{error}</div>}

          <textarea
            value={prompt}
            placeholder="Describe the class, DTO, API contract, or repository you want to generate..."
            onChange={(event) => setPrompt(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' && !event.shiftKey) {
                event.preventDefault()
                void handleSend()
              }
            }}
          />

          <div className="ai-class-builder__composer-footer">
            <span>Press Enter to send, Shift + Enter for a new line.</span>
            <button type="button" disabled={!canSend} onClick={() => void handleSend()}>
              {sending ? 'Sending...' : 'Send'}
            </button>
          </div>
        </div>
      </section>
    </div>
  )
}
