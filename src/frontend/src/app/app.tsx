import { Route, Routes } from 'react-router'
import { AdminLayout } from '@/components/layout/admin-layout'
import { DashboardPage } from '@/features/dashboard/dashboard-page'
import { InventoryPage } from '@/features/inventory/inventory-page'
import { ProductsPage } from '@/features/products/products-page'
import { ReportsPage } from '@/features/reports/reports-page'
import { NotFoundPage } from '@/pages/not-found-page'

export function App() {
  return (
    <Routes>
      <Route path="/" element={<AdminLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="productos" element={<ProductsPage />} />
        <Route path="inventario" element={<InventoryPage />} />
        <Route path="reportes" element={<ReportsPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
