import { useEffect, useMemo, useRef, useState } from 'react'
import { apiFetch, readErrorMessage } from '../lib/api'
import './AttendancePage.css'

type AttendancePageProps = {
  token: string
  view: 'monthly' | 'mark'
}

type AttendanceItem = {
  attendanceId: number
  employeeId: number
  employeeName: string
  date: string
  checkInTime?: string | null
  checkOutTime?: string | null
  status: number
}

type AttendanceDay = {
  date: Date
  status: number | null
  checkIn?: string
  checkOut?: string
}

const STATUS_COLORS: Record<number, string> = {
  0: 'status-present', // Present
  2: 'status-halfday', // HalfDay
  3: 'status-leave', // Leave
}

export default function AttendancePage({ token, view }: AttendancePageProps) {
  const [items, setItems] = useState<AttendanceItem[]>([])
  const [loading, setLoading] = useState(false)
  const [notification, setNotification] = useState<{
    message: string
    type: 'success' | 'error'
    visible: boolean
  } | null>(null)
  const [employeeId, setEmployeeId] = useState<number | null>(null)
  const [activeRecord, setActiveRecord] = useState<AttendanceItem | null>(null)
  const [confirmMode, setConfirmMode] = useState<'start' | 'stop' | null>(null)
  const hideTimerRef = useRef<number | null>(null)
  const clearTimerRef = useRef<number | null>(null)
  const autoStopRef = useRef<number | null>(null)

  const today = useMemo(() => new Date(), [])
  const [month] = useState(today.getMonth())
  const [year] = useState(today.getFullYear())

  const showNotification = (message: string, type: 'success' | 'error') => {
    if (hideTimerRef.current) window.clearTimeout(hideTimerRef.current)
    if (clearTimerRef.current) window.clearTimeout(clearTimerRef.current)
    setNotification({ message, type, visible: true })
    hideTimerRef.current = window.setTimeout(() => {
      setNotification((current) =>
        current ? { ...current, visible: false } : current
      )
    }, 9000)
    clearTimerRef.current = window.setTimeout(() => {
      setNotification(null)
    }, 10000)
  }

  const loadEmployeeId = async () => {
    try {
      const response = await apiFetch('/api/employees/me', {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      })
      if (!response.ok) return
      const data = await response.json()
      setEmployeeId(data.id ?? null)
    } catch {
      // ignore
    }
  }

  const loadAttendance = async () => {
    setLoading(true)
    try {
      const response = await apiFetch('/api/attendance/my', {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      })
      if (!response.ok) {
        const errorMessage = await readErrorMessage(response)
        showNotification(errorMessage, 'error')
        return
      }
      const data = (await response.json()) as AttendanceItem[]
      setItems(Array.isArray(data) ? data : [])
    } catch {
      showNotification('Failed to load attendance', 'error')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadEmployeeId()
    loadAttendance()
  }, [token])

  useEffect(() => {
    if (autoStopRef.current) {
      window.clearTimeout(autoStopRef.current)
    }
    if (!activeRecord || activeRecord.checkOutTime) return

    const now = new Date()
    const end = new Date(
      now.getFullYear(),
      now.getMonth(),
      now.getDate(),
      23,
      59,
      59,
      999
    )
    const timeout = end.getTime() - now.getTime()
    if (timeout > 0) {
      autoStopRef.current = window.setTimeout(() => {
        handleStop()
      }, timeout)
    }
  }, [activeRecord])

  const monthDays = useMemo(() => {
    const first = new Date(year, month, 1)
    const daysInMonth = new Date(year, month + 1, 0).getDate()
    const startDay = first.getDay()
    const days: (AttendanceDay | null)[] = []
    for (let i = 0; i < startDay; i += 1) {
      days.push(null)
    }
    for (let d = 1; d <= daysInMonth; d += 1) {
      const date = new Date(year, month, d)
      const item = items.find((entry) => {
        const entryDate = new Date(entry.date)
        return (
          entryDate.getFullYear() === date.getFullYear() &&
          entryDate.getMonth() === date.getMonth() &&
          entryDate.getDate() === date.getDate()
        )
      })
      days.push({
        date,
        status: item?.status ?? null,
        checkIn: item?.checkInTime ?? undefined,
        checkOut: item?.checkOutTime ?? undefined,
      })
    }
    return days
  }, [items, month, year])

  useEffect(() => {
    const todayItem = items.find((entry) => {
      const entryDate = new Date(entry.date)
      const now = new Date()
      return (
        entryDate.getFullYear() === now.getFullYear() &&
        entryDate.getMonth() === now.getMonth() &&
        entryDate.getDate() === now.getDate()
      )
    })
    setActiveRecord(todayItem ?? null)
  }, [items])

  const handleStart = async () => {
    if (!employeeId) {
      showNotification('Profile not found', 'error')
      return
    }
    const now = new Date()
    const response = await apiFetch('/api/attendance', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({
        employeeId,
        date: now.toISOString(),
        checkInTime: now.toTimeString().slice(0, 8),
        checkOutTime: null,
        status: 0,
      }),
    })
    if (!response.ok) {
      const errorMessage = await readErrorMessage(response)
      showNotification(errorMessage, 'error')
      return
    }
    showNotification('Attendance started', 'success')
    setConfirmMode(null)
    loadAttendance()
  }

  const handleStop = async () => {
    if (!activeRecord) return
    const now = new Date()
    const response = await apiFetch(`/api/attendance/${activeRecord.attendanceId}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({
        attendanceId: activeRecord.attendanceId,
        employeeId: activeRecord.employeeId,
        date: activeRecord.date,
        checkInTime: activeRecord.checkInTime,
        checkOutTime: now.toTimeString().slice(0, 8),
        status: activeRecord.status ?? 0,
      }),
    })
    if (!response.ok) {
      const errorMessage = await readErrorMessage(response)
      showNotification(errorMessage, 'error')
      return
    }
    showNotification('Attendance stopped', 'success')
    setConfirmMode(null)
    loadAttendance()
  }

  const isCheckedIn = Boolean(activeRecord && !activeRecord.checkOutTime)

  return (
    <div className="attendance-page">
      {notification && (
        <div
          className={`notification ${notification.type} ${
            notification.visible ? 'show' : ''
          }`}
        >
          {notification.message}
        </div>
      )}

      <div className="attendance-layout">
        <aside className="attendance-info">
          <h3>Attendance Info</h3>
          <div className="legend">
            <div className="legend-item">
              <span className="legend-dot status-present" /> Attended
            </div>
            <div className="legend-item">
              <span className="legend-dot status-halfday" /> Half Day
            </div>
            <div className="legend-item">
              <span className="legend-dot status-leave" /> Leave
            </div>
          </div>

          <div className="time-card">
            <div>
              <p>Check In</p>
              <strong>{activeRecord?.checkInTime ?? '--:--'}</strong>
            </div>
            <div>
              <p>Check Out</p>
              <strong>{activeRecord?.checkOutTime ?? '--:--'}</strong>
            </div>
          </div>
        </aside>

        <section className="attendance-content">
          {view === 'monthly' && (
            <div className="calendar-card">
              <div className="calendar-header">
                <h3>
                  {new Date(year, month).toLocaleString(undefined, {
                    month: 'long',
                    year: 'numeric',
                  })}
                </h3>
              </div>
              <div className="calendar-grid">
                {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map((day) => (
                  <div key={day} className="calendar-day header">
                    {day}
                  </div>
                ))}
                {monthDays.map((day, index) => {
                  if (!day) {
                    return <div key={`empty-${index}`} className="calendar-day empty" />
                  }
                  const statusClass =
                    day.status !== null ? STATUS_COLORS[day.status] : ''
                  const title =
                    day.checkIn || day.checkOut
                      ? `In: ${day.checkIn ?? '--'} | Out: ${day.checkOut ?? '--'}`
                      : ''
                  return (
                    <div
                      key={day.date.toISOString()}
                      className={`calendar-day ${statusClass}`}
                      title={title}
                    >
                      {day.date.getDate()}
                    </div>
                  )
                })}
              </div>
            </div>
          )}

          {view === 'mark' && (
            <div className="mark-card">
              <div className="mark-info">
                <h3>Mark Attendance</h3>
                <p>
                  Click to {isCheckedIn ? 'stop' : 'start'} today&apos;s
                  attendance.
                </p>
              </div>
              <button
                type="button"
                className={`mark-btn ${isCheckedIn ? 'active' : 'inactive'}`}
                onClick={() => setConfirmMode(isCheckedIn ? 'stop' : 'start')}
              >
                <span className="power-icon" aria-hidden="true">
                  ⏻
                </span>
              </button>
            </div>
          )}
        </section>
      </div>

      {confirmMode && (
        <div className="attendance-modal">
          <div className="modal-card">
            <h4>
              {confirmMode === 'start'
                ? 'Start Attendance?'
                : 'Stop Attendance?'}
            </h4>
            <p>
              {confirmMode === 'start'
                ? 'Do you want to start attendance now?'
                : 'Do you want to stop attendance now?'}
            </p>
            <div className="modal-actions">
              <button
                type="button"
                className="primary-btn"
                onClick={confirmMode === 'start' ? handleStart : handleStop}
              >
                Confirm
              </button>
              <button
                type="button"
                className="cancel-btn"
                onClick={() => setConfirmMode(null)}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
      {loading && <div className="attendance-loading">Loading attendance...</div>}
    </div>
  )
}
