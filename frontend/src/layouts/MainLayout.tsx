import { Link, NavLink, Outlet } from 'react-router-dom'
import { useAuthStore } from '../stores/authStore'
import { Button } from '../components/common/ui'
import { authApi } from '../features/auth/api'

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `text-sm transition ${isActive ? 'text-forest font-semibold' : 'text-muted hover:text-ink'}`

export function MainLayout() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const user = useAuthStore((s) => s.user)
  const refreshToken = useAuthStore((s) => s.refreshToken)
  const logout = useAuthStore((s) => s.logout)

  const handleLogout = async () => {
    try {
      if (refreshToken) await authApi.logout(refreshToken)
    } catch {
      // ignore logout API errors
    } finally {
      logout()
    }
  }

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
                <NavLink to="/purchases" className={linkClass}>
                  Mua
                </NavLink>
                <NavLink to="/sales" className={linkClass}>
                  Bán
                </NavLink>
                <NavLink to="/settings" className={linkClass}>
                  Cài đặt
                </NavLink>
                {(user?.role === 'Shipper' || user?.role === 'Admin') && (
                  <NavLink to="/shipper/orders" className={linkClass}>
                    Đơn shipper
                  </NavLink>
                )}
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
              <>
                <Link to="/profile" className="hidden text-sm text-muted sm:block">
                  {user?.fullName}
                </Link>
                <Button variant="ghost" onClick={handleLogout}>
                  Đăng xuất
                </Button>
              </>
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
            <NavLink to="/settings" className={linkClass}>
              Cài đặt
            </NavLink>
            {(user?.role === 'Shipper' || user?.role === 'Admin') && (
              <NavLink to="/shipper/orders" className={linkClass}>
                Shipper
              </NavLink>
            )}
          </div>
        )}
      </header>

      <Outlet />
    </div>
  )
}
