import { Navigate, Route, Routes } from 'react-router-dom'
import { MainLayout } from '../layouts/MainLayout'
import { HomePage } from '../pages/HomePage'
import { LoginPage } from '../pages/LoginPage'
import { RegisterPage } from '../pages/RegisterPage'
import { ProductDetailPage } from '../pages/ProductDetailPage'
import { CreateProductPage } from '../pages/CreateProductPage'
import { EditProductPage } from '../pages/EditProductPage'
import { MyProductsPage } from '../pages/MyProductsPage'
import { FavoritesPage } from '../pages/FavoritesPage'
import { PurchasesPage, SalesPage } from '../pages/OrdersPages'
import { OrderDetailPage } from '../pages/OrderDetailPage'
import { CheckoutPage } from '../pages/CheckoutPage'
import { SettingsPage } from '../pages/SettingsPage'
import { ShipperOrdersPage } from '../pages/ShipperOrdersPage'
import { ProfilePage } from '../pages/ProfilePage'
import { AdminCategoriesPage } from '../pages/AdminCategoriesPage'
import { ProtectedRoute } from './ProtectedRoute'
import { AdminRoute } from './AdminRoute'
import { ShipperRoute } from './ShipperRoute'

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route index element={<HomePage />} />
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
        <Route
          path="products/new"
          element={
            <ProtectedRoute>
              <CreateProductPage />
            </ProtectedRoute>
          }
        />
        <Route path="products/:id" element={<ProductDetailPage />} />
        <Route
          path="products/:id/edit"
          element={
            <ProtectedRoute>
              <EditProductPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="my-products"
          element={
            <ProtectedRoute>
              <MyProductsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="favorites"
          element={
            <ProtectedRoute>
              <FavoritesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="purchases"
          element={
            <ProtectedRoute>
              <PurchasesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="sales"
          element={
            <ProtectedRoute>
              <SalesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="orders/:id"
          element={
            <ProtectedRoute>
              <OrderDetailPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="checkout/:productId"
          element={
            <ProtectedRoute>
              <CheckoutPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="settings"
          element={
            <ProtectedRoute>
              <SettingsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="shipper/orders"
          element={
            <ShipperRoute>
              <ShipperOrdersPage />
            </ShipperRoute>
          }
        />
        <Route
          path="profile"
          element={
            <ProtectedRoute>
              <ProfilePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin/categories"
          element={
            <AdminRoute>
              <AdminCategoriesPage />
            </AdminRoute>
          }
        />

        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}
