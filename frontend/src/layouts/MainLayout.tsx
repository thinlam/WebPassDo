import { Link, NavLink, Outlet } from 'react-router-dom'
import { useEffect, useRef, useState } from 'react'
import { useAuthStore } from '../stores/authStore'
import { Button } from '../components/common/ui'
import { ErrorBoundary } from '../components/common/ErrorBoundary'
import { authApi } from '../features/auth/api'
import { PresenceProvider } from '../features/presence/PresenceProvider'

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `text-sm transition ${isActive ? 'text-forest font-semibold' : 'text-muted hover:text-ink'}`

export function MainLayout() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const user = useAuthStore((s) => s.user)
  const refreshToken = useAuthStore((s) => s.refreshToken)
  const logout = useAuthStore((s) => s.logout)
  const [dropdownOpen, setDropdownOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  const handleLogout = async () => {
    try {
      if (refreshToken) await authApi.logout(refreshToken)
    } catch {
      // ignore logout API errors
    } finally {
      logout()
    }
  }

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setDropdownOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-20 border-b border-line/80 bg-paper/90 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-4 md:px-6">
          <Link to="/" className="font-display text-2xl text-forest">
            PassDo
          </Link>

          <nav className="hidden items-center gap-5 md:flex">
            <NavLink to="/" end className={linkClass}>
              Khám phá
            </NavLink>
            {isAuthenticated && (
              <>
                <NavLink to="/products/new" className={linkClass}>
                  Đăng bán
                </NavLink>
                <NavLink to="/my-products" className={linkClass}>
                  Đồ của tôi
                </NavLink>
                <NavLink to="/favorites" className={linkClass}>
                  Yêu thích
                </NavLink>
                <NavLink to="/messages" className={linkClass}>
                  Tin nhắn
                </NavLink>
                {user?.role === 'Admin' && (
                  <NavLink to="/admin/categories" className={linkClass}>
                    Danh mục
                  </NavLink>
                )}
              </>
            )}
          </nav>

          <div className="flex items-center gap-2">
            {isAuthenticated ? (
              <div className="relative" ref={dropdownRef}>
                <button
                  onClick={() => setDropdownOpen((v) => !v)}
                  className="hidden items-center gap-2 rounded-md px-3 py-1.5 text-sm text-muted transition hover:bg-sand hover:text-ink sm:flex"
                >
                  {user?.avatarUrl && (
                    <img src={user.avatarUrl} alt="" className="h-6 w-6 rounded-full object-cover" />
                  )}
                  <span>{user?.fullName}</span>
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
                  </svg>
                </button>
                {dropdownOpen && (
                  <div className="absolute right-0 top-full z-30 mt-1 w-56 rounded-xl border border-line bg-white py-1 shadow-lg">
                    <DropdownLink to="/profile" onClick={() => setDropdownOpen(false)}>Thông tin cá nhân</DropdownLink>
                    <DropdownLink to="/settings?tab=addresses" onClick={() => setDropdownOpen(false)}>Địa chỉ của tôi</DropdownLink>
                    <DropdownLink to="/settings?tab=banks" onClick={() => setDropdownOpen(false)}>Tài khoản ngân hàng</DropdownLink>
                    <div className="my-1 border-t border-line" />
                    <DropdownLink to="/purchases" onClick={() => setDropdownOpen(false)}>Đơn mua</DropdownLink>
                    <DropdownLink to="/sales" onClick={() => setDropdownOpen(false)}>Đơn bán</DropdownLink>
                    <DropdownLink to="/messages" onClick={() => setDropdownOpen(false)}>Tin nhắn</DropdownLink>
                    <div className="my-1 border-t border-line" />
                    <DropdownLink to="/account/security" onClick={() => setDropdownOpen(false)}>Tài khoản & Bảo mật</DropdownLink>
                    <DropdownLink to="/support" onClick={() => setDropdownOpen(false)}>Trung tâm hỗ trợ</DropdownLink>
                    <div className="my-1 border-t border-line" />
                    <button
                      onClick={() => { setDropdownOpen(false); handleLogout() }}
                      className="w-full px-4 py-2 text-left text-sm text-rose-700 hover:bg-sand transition"
                    >
                      Đăng xuất
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <>
                <Link to="/login">
                  <Button variant="ghost">Đăng nhập</Button>
                </Link>
                <Link to="/register">
                  <Button>Đăng ký</Button>
                </Link>
              </>
            )}
          </div>
        </div>

        {isAuthenticated && (
          <div className="flex gap-4 overflow-x-auto border-t border-line px-4 py-2 md:hidden">
            <NavLink to="/products/new" className={linkClass}>
              Đăng bán
            </NavLink>
            <NavLink to="/my-products" className={linkClass}>
              Của tôi
            </NavLink>
            <NavLink to="/favorites" className={linkClass}>
              Yêu thích
            </NavLink>
            <NavLink to="/purchases" className={linkClass}>
              Mua
            </NavLink>
            <NavLink to="/sales" className={linkClass}>
              Bán
            </NavLink>
            <NavLink to="/messages" className={linkClass}>
              Tin nhắn
            </NavLink>
            <NavLink to="/settings" className={linkClass}>
              Cài đặt
            </NavLink>
          </div>
        )}
      </header>

      {isAuthenticated && <PresenceProvider />}

      <ErrorBoundary>
        <Outlet />
      </ErrorBoundary>
    </div>
  )
}

function DropdownLink({ to, children, onClick }: { to: string; children: React.ReactNode; onClick: () => void }) {
  return (
    <Link to={to} onClick={onClick} className="block px-4 py-2 text-sm text-ink hover:bg-sand transition">
      {children}
    </Link>
  )
}
