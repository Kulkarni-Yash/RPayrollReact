import { useEffect, useMemo, useState } from 'react'
import Header from '../components/Header'
import DashboardPage from '../pages/DashboardPage'
import EmployeePage from '../pages/EmployeePage'
import AttendancePage from '../pages/AttendancePage'
import ProfilePage from '../pages/ProfilePage'
import { apiFetch } from '../lib/api'
import './HomeLayout.css'

type HomeLayoutProps = {
  token: string
  username: string
  role: string
  onLogout: () => void
}

type MenuItem = {
  key: string
  label: string
}

const fallbackMenu: MenuItem[] = [
  { key: 'dashboard', label: 'Dashboard' },
  { key: 'employee', label: 'Employee' },
]

export default function HomeLayout({ token, username, role, onLogout }: HomeLayoutProps) {

  const [menuItems, setMenuItems] = useState<MenuItem[]>(fallbackMenu)
  const [activePage, setActivePage] = useState<string>(fallbackMenu[0].key)

  /* NEW STATE */
  const [employeeView, setEmployeeView] = useState<'list' | 'create'>('list')
  const [attendanceView, setAttendanceView] = useState<'monthly' | 'mark'>('monthly')

  useEffect(() => {
    let isActive = true

    const loadMenu = async () => {
      try {
        const response = await apiFetch('/api/menu', {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        })

        if (!response.ok) return

        const data = (await response.json()) as MenuItem[]

        if (!isActive || !Array.isArray(data) || data.length === 0) return

        setMenuItems(data)

        setActivePage((current) =>
          data.some((item) => item.key === current) ? current : data[0].key
        )
      } catch {
        // keep fallback
      }
    }

    loadMenu()

    return () => {
      isActive = false
    }
  }, [token])

  const pageTitle = useMemo(() => {
    if (activePage === 'profile') {
      return 'My Profile'
    }
    if (activePage === 'attendance') {
      return 'Attendance'
    }
    const match = menuItems.find((item) => item.key === activePage)
    return match?.label ?? 'Dashboard'
  }, [activePage, menuItems])

  const visibleMenu = useMemo(() => {
    if (role.toLowerCase() === 'employee') {
      return menuItems.filter((item) => item.key !== 'employee')
    }
    return menuItems
  }, [menuItems, role])

  const canViewProfile = role.toLowerCase() === 'employee'

  useEffect(() => {
    if (activePage === 'profile') {
      return
    }
    if (!visibleMenu.some((item) => item.key === activePage)) {
      setActivePage(visibleMenu[0]?.key ?? 'dashboard')
    }
  }, [activePage, visibleMenu])

  return (
    <div className="app-shell">

      <Header
        activePage={activePage}
        menuItems={visibleMenu}
        onNavigate={setActivePage}
        username={username}
        onLogout={onLogout}
        onProfile={() => setActivePage('profile')}
        showProfile={canViewProfile}
      />

      <div className="app-body">

        {/* TITLE CARD */}
        <div className="page-title-bar">

          <div className="title-left">
            {pageTitle}
          </div>

          {/* EMPLOYEE ACTION BUTTONS */}
          {activePage === 'employee' && (
            <div className="title-actions">

              <button
                className={employeeView === 'list' ? 'active' : ''}
                onClick={() => setEmployeeView('list')}
              >
                <span className="action-icon">☰</span>
                <span className="action-text">List Employees</span>
              </button>

              <button
                className={employeeView === 'create' ? 'active' : ''}
                onClick={() => setEmployeeView('create')}
              >
                <span className="action-icon">+</span>
                <span className="action-text">Create Employee</span>
              </button>

            </div>
          )}

          {activePage === 'attendance' && (
            <div className="title-actions">
              <button
                className={attendanceView === 'monthly' ? 'active' : ''}
                onClick={() => setAttendanceView('monthly')}
              >
                <span className="action-icon">☰</span>
                <span className="action-text">Monthly Attendance</span>
              </button>

              <button
                className={attendanceView === 'mark' ? 'active' : ''}
                onClick={() => setAttendanceView('mark')}
              >
                <span className="action-icon">+</span>
                <span className="action-text">Mark Attendance</span>
              </button>
            </div>
          )}

        </div>

        {/* CONTENT CARD */}

        <div className="page-content">

          {activePage === 'dashboard' && <DashboardPage />}

          {activePage === 'employee' && (
            <EmployeePage
              view={employeeView}
              onChangeView={setEmployeeView}
              token={token}
            />
          )}

          {activePage === 'attendance' && (
            <AttendancePage
              token={token}
              view={attendanceView}
            />
          )}

          {activePage === 'profile' && canViewProfile && (
            <ProfilePage token={token} />
          )}

          {activePage !== 'dashboard' &&
            activePage !== 'employee' &&
            activePage !== 'attendance' &&
            activePage !== 'profile' && (
            <div>Page coming soon.</div>
          )}

        </div>

      </div>

    </div>
  )
}
