import { Activity, ArrowRight, Boxes, Building2, CircleCheck, Clock3, Package } from 'lucide-react'
import { Link } from 'react-router'
import { navigationItems } from '@/app/navigation'
import { EmptyState } from '@/components/empty-state'
import { PageHeading } from '@/components/page-heading'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

const indicators = [
  { title: 'Productos en catálogo', description: 'Consulta pendiente de integración', icon: Package },
  { title: 'Sucursales', description: 'Sin contexto de negocio conectado', icon: Building2 },
  { title: 'Alertas de inventario', description: 'No se han consultado existencias', icon: Boxes },
]

export function DashboardPage() {
  return (
    <div className="space-y-6">
      <PageHeading title="Dashboard" description="Un espacio para consultar el catálogo, revisar inventario y acceder a información administrativa." />

      <section aria-label="Indicadores administrativos" className="grid gap-4 md:grid-cols-3">
        {indicators.map(({ title, description, icon: Icon }) => (
          <Card key={title}>
            <CardHeader className="flex flex-row items-start justify-between gap-3 pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
              <Icon className="size-5 shrink-0 text-primary" aria-hidden="true" />
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-semibold"><span aria-hidden="true">—</span><span className="sr-only">Dato no disponible</span></p>
              <p className="mt-2 text-xs leading-relaxed text-muted-foreground">{description}</p>
            </CardContent>
          </Card>
        ))}
      </section>

      <section aria-labelledby="dashboard-modulos" className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h2 id="dashboard-modulos" className="text-lg font-semibold">Explora tu espacio de trabajo</h2>
          <Badge variant="outline">3 módulos administrativos</Badge>
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          {navigationItems.filter((item) => item.href !== '/').map(({ href, label, description, icon: Icon }) => (
            <Card key={href} className="flex flex-col">
              <CardHeader className="flex-1">
                <div className="mb-2 flex size-11 items-center justify-center rounded-xl bg-accent text-primary">
                  <Icon className="size-5" aria-hidden="true" />
                </div>
                <CardTitle>{label}</CardTitle>
                <CardDescription>{description}. Pantalla base, sin operaciones reales.</CardDescription>
              </CardHeader>
              <CardContent>
                <Button asChild variant="outline" className="w-full justify-between">
                  <Link to={href}>Abrir {label}<ArrowRight aria-hidden="true" /></Link>
                </Button>
              </CardContent>
            </Card>
          ))}
        </div>
      </section>

      <div className="grid items-start gap-6 xl:grid-cols-[1.5fr_1fr]">
        <Card>
          <CardHeader className="border-b">
            <CardTitle>Actividad administrativa</CardTitle>
            <CardDescription>Las operaciones y su historial se consultarán desde servicios autorizados.</CardDescription>
          </CardHeader>
          <CardContent className="pb-0">
            <EmptyState icon={Activity} title="Sin actividad conectada" description="Todavía no existe una fuente de datos para esta vista. Este estado no significa que el negocio no tenga operaciones." />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Estado de esta versión</CardTitle>
            <CardDescription>Qué puedes explorar en FRONT-01.</CardDescription>
          </CardHeader>
          <CardContent>
            <ul className="space-y-5 text-sm">
              <li className="flex items-start gap-3">
                <CircleCheck className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
                <div><p className="font-medium">Navegación y diseño responsive</p><p className="mt-1 text-muted-foreground">Cuatro secciones para desktop, tablet y móvil.</p></div>
              </li>
              <li className="flex items-start gap-3">
                <CircleCheck className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
                <div><p className="font-medium">Ejemplo de catálogo</p><p className="mt-1 text-muted-foreground">Demostración local en Productos, con búsqueda y filtros.</p></div>
              </li>
              <li className="flex items-start gap-3">
                <Clock3 className="mt-0.5 size-5 shrink-0 text-muted-foreground" aria-hidden="true" />
                <div><p className="font-medium">Integración de negocio pendiente</p><p className="mt-1 text-muted-foreground">Datos, autenticación y operaciones se implementarán en hitos posteriores.</p></div>
              </li>
            </ul>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
