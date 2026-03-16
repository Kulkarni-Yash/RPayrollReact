import { useEffect, useMemo, useState } from 'react'
import Header from '../components/Header'
import DashboardPage from '../pages/DashboardPage'
import EmployeePage from '../pages/EmployeePage'
import AttendancePage from '../pages/AttendancePage'
import FormBuilderPage from '../pages/FormBuilderPage'
import DynamicFormPage from '../pages/DynamicFormPage'
import MenuManagementPage from '../pages/MenuManagementPage'
import ProfilePage from '../pages/ProfilePage'
import AIClassBuilderPage from '../pages/AIClassBuilderPage'
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
  const [menuRefreshToken, setMenuRefreshToken] = useState(0)
  const [formBuilderEditTarget, setFormBuilderEditTarget] = useState<string | null>(null)

  /* NEW STATE */
  const [employeeView, setEmployeeView] = useState<'list' | 'create'>('list')
  const [attendanceView, setAttendanceView] = useState<'monthly' | 'mark'>('monthly')
  const [formBuilderView, setFormBuilderView] = useState<'create' | 'view'>('create')

  const refreshMenuItems = () => {
    setMenuRefreshToken((current) => current + 1)
  }

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
  }, [token, menuRefreshToken])

  const pageTitle = useMemo(() => {
    if (activePage === 'profile') {
      return 'My Profile'
    }
    if (activePage === 'attendance') {
      return 'Attendance'
    }
    if (activePage === 'menus') {
      return 'Menu Items'
    }
    if (activePage === 'form-builder') {
      return 'Form Builder'
    }
    if (activePage === 'ai-class-builder') {
      return 'AI Class Builder'
    }
    const match = menuItems.find((item) => item.key === activePage)
    return match?.label ?? 'Dashboard'
  }, [activePage, menuItems])

  const visibleMenu = useMemo(() => {
    const baseItems = role.toLowerCase() === 'employee'
      ? menuItems.filter((item) => item.key !== 'employee')
      : menuItems

    if (role.toLowerCase() === 'admin' && !baseItems.some((item) => item.key === 'menus')) {
      return [...baseItems, { key: 'menus', label: 'Menus' }]
    }
    return baseItems
  }, [menuItems, role])

  const canViewProfile = role.toLowerCase() === 'employee'
  const canViewFormBuilder = role.toLowerCase() === 'admin'
  const canViewAiClassBuilder = role.toLowerCase() === 'admin'
  const canViewMenus = role.toLowerCase() === 'admin'

  useEffect(() => {
    if (activePage === 'profile' || activePage === 'form-builder' || activePage === 'ai-class-builder' || activePage === 'menus') {
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
        onNavigate={(page) => {
          setFormBuilderEditTarget(null)
          setActivePage(page)
        }}
        username={username}
        onLogout={onLogout}
        onProfile={() => setActivePage('profile')}
        onFormBuilder={() => {
          setFormBuilderEditTarget(null)
          setActivePage('form-builder')
        }}
        onAiClassBuilder={() => setActivePage('ai-class-builder')}
        showProfile={canViewProfile}
        showFormBuilder={canViewFormBuilder}
        showAiClassBuilder={canViewAiClassBuilder}
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

          {activePage === 'form-builder' && (
            <div className="title-actions">
              <button
                className={formBuilderView === 'create' ? 'active' : ''}
                onClick={() => setFormBuilderView('create')}
              >
                <span className="action-icon">+</span>
                <span className="action-text">Create</span>
              </button>

              <button
                className={formBuilderView === 'view' ? 'active' : ''}
                onClick={() => setFormBuilderView('view')}
              >
                <span className="action-icon">☰</span>
                <span className="action-text">View</span>
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

          {activePage === 'form-builder' && (
            <FormBuilderPage
              view={formBuilderView}
              token={token}
              editLayoutKey={formBuilderEditTarget}
              onMenusChanged={refreshMenuItems}
            />
          )}

          {activePage === 'ai-class-builder' && canViewAiClassBuilder && (
            <AIClassBuilderPage username={username} token={token} />
          )}

          {activePage === 'menus' && canViewMenus && (
            <MenuManagementPage
              token={token}
              onMenusChanged={refreshMenuItems}
              onEditForm={(layoutKey) => {
                setFormBuilderEditTarget(layoutKey)
                setFormBuilderView('create')
                setActivePage('form-builder')
              }}
            />
          )}

          {activePage === 'profile' && canViewProfile && (
            <ProfilePage token={token} />
          )}

          {activePage !== 'dashboard' &&
            activePage !== 'employee' &&
            activePage !== 'attendance' &&
            activePage !== 'form-builder' &&
            activePage !== 'ai-class-builder' &&
            activePage !== 'menus' &&
            activePage !== 'profile' && (
            <DynamicFormPage token={token} menuKey={activePage} />
          )}

        </div>

      </div>

    </div>
  )
}
