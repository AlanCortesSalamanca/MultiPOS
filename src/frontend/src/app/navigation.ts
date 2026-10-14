import { BarChart3, Boxes, LayoutDashboard, Package, type LucideIcon } from 'lucide-react'

type NavigationItem = {
  href: string
  label: string
  description: string
  icon: LucideIcon
}

export const navigationItems: NavigationItem[] = [
  { href: '/', label: 'Dashboard', description: 'Vista general de la administración', icon: LayoutDashboard },
  { href: '/productos', label: 'Productos', description: 'Catálogo, categorías y unidades', icon: Package },
  { href: '/inventario', label: 'Inventario', description: 'Existencias y movimientos por sucursal', icon: Boxes },
  { href: '/reportes', label: 'Reportes', description: 'Consultas e información administrativa', icon: BarChart3 },
]
