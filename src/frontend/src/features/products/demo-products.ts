// Datos de presentación exclusivos de FRONT-01. No son DTOs ni registros reales.
type DemoProduct = {
  sku: string
  name: string
  category: string
  unit: string
  status: 'Activo' | 'Inactivo'
}

export const demoProducts: readonly DemoProduct[] = [
  { sku: 'DEMO-001', name: 'Guante de trabajo', category: 'Seguridad', unit: 'Par', status: 'Activo' },
  { sku: 'DEMO-002', name: 'Cable eléctrico', category: 'Electricidad', unit: 'Metro', status: 'Activo' },
  { sku: 'DEMO-003', name: 'Cinta aislante', category: 'Electricidad', unit: 'Pieza', status: 'Activo' },
  { sku: 'DEMO-004', name: 'Llave ajustable', category: 'Herramientas', unit: 'Pieza', status: 'Inactivo' },
]

export const demoCategories = [...new Set(demoProducts.map((product) => product.category))]
