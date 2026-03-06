import { useEffect, useMemo, useState } from 'react'
import Header from '../components/Header'
import DashboardPage from '../pages/DashboardPage'
import EmployeePage from '../pages/EmployeePage'
import { apiFetch } from '../lib/api'
import './HomeLayout.css'

type HomeLayoutProps = {
  token: string
  username: string
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

export default function HomeLayout({ token, username, onLogout }: HomeLayoutProps) {

  const [menuItems, setMenuItems] = useState<MenuItem[]>(fallbackMenu)
  const [activePage, setActivePage] = useState<string>(fallbackMenu[0].key)

  /* NEW STATE */
  const [employeeView, setEmployeeView] = useState<'list' | 'create'>('list')

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
    const match = menuItems.find((item) => item.key === activePage)
    return match?.label ?? 'Dashboard'
  }, [activePage, menuItems])

  return (
    <div className="app-shell">

      <Header
        activePage={activePage}
        menuItems={menuItems}
        onNavigate={setActivePage}
        username={username}
        onLogout={onLogout}
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

          {activePage !== 'dashboard' && activePage !== 'employee' && (
            <div>Page coming soon.</div>
          )}

        </div>

      </div>

    </div>
  )
}
