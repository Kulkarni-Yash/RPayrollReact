import { useState } from 'react'
import './Header.css'

export type MenuItem = {
  key: string
  label: string
}

type HeaderProps = {
  activePage: string
  menuItems: MenuItem[]
  onNavigate: (page: string) => void
  username: string
  onLogout: () => void
}

export default function Header({
  activePage,
  menuItems,
  onNavigate,
  username,
  onLogout,
}: HeaderProps) {
  const [menuOpen, setMenuOpen] = useState(false)

  return (
    <header className="app-header">
      <div className="header-left">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true">
            RP
          </span>
          <span className="brand-name">RPayroll</span>
        </div>

        <nav className="header-nav">
          {menuItems.map((item) => (
            <button
              key={item.key}
              type="button"
              className={activePage === item.key ? 'active' : ''}
              onClick={() => onNavigate(item.key)}
            >
              {item.label}
            </button>
          ))}
        </nav>
      </div>

      <button type="button" className="header-menu-btn" aria-label="Open menu">
        ☰
      </button>

      <div className="header-user-menu">
        <button
          type="button"
          className="header-user-btn"
          onClick={() => setMenuOpen((prev) => !prev)}
        >
          Hi, {username}
        </button>

        {menuOpen && (
          <div className="header-user-dropdown">
            <button type="button" onClick={onLogout}>
              Log out
            </button>
          </div>
        )}
      </div>
    </header>
  )
}
