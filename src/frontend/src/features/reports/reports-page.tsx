import { BarChart3, Boxes, Download, FileBarChart, History, ShoppingBag } from 'lucide-react'
import { EmptyState } from '@/components/empty-state'
import { PageHeading } from '@/components/page-heading'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

const plannedReports = [
  { title: 'Resumen de ventas', description: 'Consulta administrativa de operaciones históricas por periodo y sucursal.', icon: BarChart3 },
  { title: 'Inventario por sucursal', description: 'Consulta de existencias y disponibilidad informadas por el backend.', icon: Boxes },
  { title: 'Movimientos de inventario', description: 'Trazabilidad de entradas, salidas, ajustes y traspasos.', icon: History },
  { title: 'Pedidos y compras', description: 'Consulta administrativa de abastecimiento y diferencias de recepción.', icon: ShoppingBag },
]

export function ReportsPage() {
  return (
    <div className="space-y-6">
      <PageHeading title="Reportes" description="Un punto de entrada para futuras consultas administrativas. No se generan reportes ni se exportan documentos en FRONT-01." />

      <Card>
        <CardHeader className="border-b">
          <CardTitle>Resultados de reportes</CardTitle>
          <CardDescription>Los filtros por fecha y sucursal se implementarán junto con sus contratos de consulta.</CardDescription>
        </CardHeader>
        <CardContent className="pb-0">
          <EmptyState icon={FileBarChart} title="Sin reportes conectados" description="No se ha consultado información del negocio. Las tarjetas siguientes describen vistas previstas; no contienen resultados reales ni datos ilustrativos." />
        </CardContent>
      </Card>

      <section aria-labelledby="reportes-previstos" className="space-y-4">
        <h2 id="reportes-previstos" className="text-lg font-semibold">Vistas previstas</h2>
        <div className="grid gap-4 md:grid-cols-2">
          {plannedReports.map(({ title, description, icon: Icon }, index) => (
            <Card key={title} className="flex flex-col">
              <CardHeader className="flex-1">
                <div className="mb-2 flex items-center justify-between gap-3">
                  <div className="flex size-11 items-center justify-center rounded-xl bg-secondary text-primary"><Icon className="size-5" aria-hidden="true" /></div>
                  <Badge variant="outline">No implementado</Badge>
                </div>
                <CardTitle>{title}</CardTitle>
                <CardDescription>{description}</CardDescription>
              </CardHeader>
              <CardContent className="space-y-3">
                <Button variant="outline" disabled aria-label={`Exportar ${title.toLocaleLowerCase('es-MX')}, no disponible`} aria-describedby={`reporte-pendiente-${index}`}><Download aria-hidden="true" />Exportar</Button>
                <p id={`reporte-pendiente-${index}`} className="text-xs text-muted-foreground">Pendiente de contratos e integración, no de permisos locales.</p>
              </CardContent>
            </Card>
          ))}
        </div>
      </section>
    </div>
  )
}
