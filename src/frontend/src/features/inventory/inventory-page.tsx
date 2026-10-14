import { ArrowLeftRight, Boxes, ClipboardList, Download, MapPin } from 'lucide-react'
import { EmptyState } from '@/components/empty-state'
import { PageHeading } from '@/components/page-heading'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

const plannedViews = [
  { title: 'Existencias', description: 'Consulta por producto y sucursal desde la fuente central.', icon: Boxes },
  { title: 'Movimientos', description: 'Historial de entradas, salidas y ajustes de inventario.', icon: ArrowLeftRight },
  { title: 'Reposición', description: 'Consulta de pendientes según los contratos del negocio.', icon: ClipboardList },
]

export function InventoryPage() {
  return (
    <div className="space-y-6">
      <PageHeading title="Inventario" description="Base para la consulta de existencias por sucursal. Esta versión no calcula stock ni registra movimientos." actions={
        <Button variant="outline" disabled aria-describedby="inventario-pendiente"><Download aria-hidden="true" />Exportar inventario</Button>
      } />
      <p id="inventario-pendiente" className="text-sm text-muted-foreground">La consulta, exportación y cualquier operación de inventario están pendientes de integración.</p>

      <Card>
        <CardHeader className="border-b">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <CardTitle>Existencias por sucursal</CardTitle>
            <Badge variant="outline">Pendiente de integración</Badge>
          </div>
          <CardDescription>El backend será la fuente autoritativa de saldos. No se asume una sucursal ni un contexto de acceso.</CardDescription>
        </CardHeader>
        <CardContent className="pt-5">
          <div className="flex items-center gap-3 rounded-lg bg-muted px-4 py-3 text-sm text-muted-foreground">
            <MapPin className="size-5 shrink-0" aria-hidden="true" />
            <p>Sin sucursales conectadas. El selector se habilitará cuando exista contexto y una consulta autorizada.</p>
          </div>
          <EmptyState icon={Boxes} title="Existencias todavía no disponibles" description="No hay una consulta real de inventario en esta versión. La ausencia de datos no equivale a stock cero ni autoriza ajustes o traspasos." />
        </CardContent>
      </Card>

      <section aria-labelledby="inventario-vistas" className="space-y-4">
        <h2 id="inventario-vistas" className="text-lg font-semibold">Consultas previstas</h2>
        <div className="grid gap-4 md:grid-cols-3">
          {plannedViews.map(({ title, description, icon: Icon }) => (
            <Card key={title}>
              <CardHeader>
                <Icon className="mb-2 size-5 text-primary" aria-hidden="true" />
                <CardTitle>{title}</CardTitle>
                <CardDescription>{description}</CardDescription>
              </CardHeader>
              <CardContent><Badge variant="outline">No implementado</Badge></CardContent>
            </Card>
          ))}
        </div>
      </section>
    </div>
  )
}
