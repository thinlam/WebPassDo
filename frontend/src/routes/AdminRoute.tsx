import { Navigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuthStore } from '../stores/authStore'

type AdminRouteProps = {
  children: ReactNode
}

export function AdminRoute({ children }: AdminRouteProps) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const role = useAuthStore((state) => state.user?.role)

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (role !== 'Admin') {
    return <Navigate to="/" replace />
  }

  return children
}
