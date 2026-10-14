import { useRef, useState } from 'react'
import { Eye, Package, Plus, Search } from 'lucide-react'
import { EmptyState } from '@/components/empty-state'
import { PageHeading } from '@/components/page-heading'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { demoCategories, demoProducts } from '@/features/products/demo-products'

export function ProductsPage() {
  const [showDemo, setShowDemo] = useState(false)
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('')
  const demoButtonRef = useRef<HTMLButtonElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)
  const term = search.trim().toLocaleLowerCase('es-MX')
  const filteredProducts = demoProducts.filter((product) =>
    (category === '' || product.category === category) &&
    `${product.name} ${product.sku}`.toLocaleLowerCase('es-MX').includes(term),
  )

  function clearFilters() {
    setSearch('')
    setCategory('')
    searchRef.current?.focus()
  }

  function toggleDemo() {
    clearFilters()
    setShowDemo((current) => !current)
    demoButtonRef.current?.focus()
  }

  return (
    <div className="space-y-6">
      <PageHeading title="Productos" description="Pantalla base del catálogo. La consulta y la administración de productos reales todavía no están conectadas." actions={
        <>
          <Button ref={demoButtonRef} variant="outline" onClick={toggleDemo}>
            <Eye aria-hidden="true" />{showDemo ? 'Ocultar demostración' : 'Ver demostración'}
          </Button>
          <Button disabled aria-describedby="productos-pendiente"><Plus aria-hidden="true" />Crear producto</Button>
        </>
      } />

      <p id="productos-pendiente" className="text-sm text-muted-foreground">Crear y editar productos está pendiente de contratos e integración. No se guarda información en esta versión.</p>

      <Card>
        <CardHeader className="border-b">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <CardTitle>Catálogo de productos</CardTitle>
            <Badge variant={showDemo ? 'warning' : 'outline'}>{showDemo ? 'Datos de demostración' : 'Pendiente de integración'}</Badge>
          </div>
          <CardDescription id="productos-ayuda">
            {showDemo
              ? 'Ejemplos ficticios, solo para explorar la interfaz. Los filtros actúan sobre esta lista local; no consultan el negocio.'
              : 'Puedes activar la demostración para probar la búsqueda. Los filtros se habilitarán únicamente sobre datos ilustrativos.'}
          </CardDescription>
        </CardHeader>
        <CardContent className="pt-5">
          <div className="grid items-end gap-4 sm:grid-cols-[minmax(0,1fr)_minmax(12rem,0.6fr)_auto]">
            <div className="space-y-2">
              <label htmlFor="producto-busqueda" className="text-sm font-medium">Buscar por nombre o SKU</label>
              <div className="relative">
                <Search className="pointer-events-none absolute top-3.5 left-3 size-4 text-muted-foreground" aria-hidden="true" />
                <Input ref={searchRef} id="producto-busqueda" type="search" value={search} onChange={(event) => setSearch(event.target.value)} disabled={!showDemo} placeholder="Ejemplo: cable o DEMO-002" className="pl-9" aria-describedby="productos-ayuda" />
              </div>
            </div>
            <div className="space-y-2">
              <label htmlFor="producto-categoria" className="text-sm font-medium">Categoría</label>
              <select id="producto-categoria" value={category} onChange={(event) => setCategory(event.target.value)} disabled={!showDemo} aria-describedby="productos-ayuda" className="h-11 w-full rounded-md border border-input bg-card px-3 text-base disabled:cursor-not-allowed disabled:bg-muted disabled:opacity-60 md:text-sm">
                <option value="">Todas las categorías</option>
                {showDemo && demoCategories.map((item) => <option key={item} value={item}>{item}</option>)}
              </select>
            </div>
            <Button variant="outline" onClick={clearFilters} disabled={!showDemo || (search === '' && category === '')}>Limpiar filtros</Button>
          </div>

          <p role="status" className="mt-4 text-xs text-muted-foreground">
            {showDemo ? `${filteredProducts.length} de ${demoProducts.length} ejemplos ficticios visibles. No son datos reales.` : 'Sin fuente de datos conectada.'}
          </p>

          {!showDemo ? (
            <EmptyState icon={Package} title="Catálogo todavía no conectado" description="No se han consultado productos reales. Activa la demostración para explorar cómo se verá el listado, sin crear ni modificar datos." action={<Button variant="outline" onClick={toggleDemo}>Ver datos de demostración</Button>} />
          ) : filteredProducts.length === 0 ? (
            <EmptyState icon={Search} title="Sin coincidencias en la demostración" description="Prueba otro nombre o SKU, o limpia los filtros para ver todos los ejemplos ficticios." action={<Button variant="outline" onClick={clearFilters}>Limpiar filtros</Button>} />
          ) : (
            <div tabIndex={0} role="region" aria-label="Tabla de productos de demostración, desplazable horizontalmente" className="mt-5 overflow-x-auto rounded-lg border">
              <table className="w-full min-w-[640px] text-left text-sm">
                <caption className="sr-only">Productos ficticios de demostración. No representan el catálogo real del negocio.</caption>
                <thead className="border-b bg-muted text-xs text-muted-foreground">
                  <tr>
                    <th scope="col" className="px-4 py-3 font-medium">SKU de ejemplo</th>
                    <th scope="col" className="px-4 py-3 font-medium">Producto</th>
                    <th scope="col" className="px-4 py-3 font-medium">Categoría</th>
                    <th scope="col" className="px-4 py-3 font-medium">Unidad</th>
                    <th scope="col" className="px-4 py-3 font-medium">Estado ilustrativo</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {filteredProducts.map((product) => (
                    <tr key={product.sku} className="hover:bg-muted/50">
                      <td className="px-4 py-4 font-mono text-xs text-muted-foreground">{product.sku}</td>
                      <th scope="row" className="px-4 py-4 font-medium">{product.name}</th>
                      <td className="px-4 py-4 text-muted-foreground">{product.category}</td>
                      <td className="px-4 py-4 text-muted-foreground">{product.unit}</td>
                      <td className="px-4 py-4"><Badge variant={product.status === 'Activo' ? 'success' : 'secondary'}>{product.status}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
