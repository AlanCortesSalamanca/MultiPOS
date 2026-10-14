import { ArrowLeft, FileQuestion } from 'lucide-react'
import { Link } from 'react-router'
import { EmptyState } from '@/components/empty-state'
import { PageHeading } from '@/components/page-heading'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'

export function NotFoundPage() {
  return (
    <div className="space-y-6">
      <PageHeading title="Página no encontrada" description="Esta dirección no corresponde a una sección de la interfaz administrativa." />
      <Card>
        <EmptyState icon={FileQuestion} title="No encontramos esta sección" description="Utiliza el menú para abrir Dashboard, Productos, Inventario o Reportes." action={
          <Button asChild><Link to="/"><ArrowLeft aria-hidden="true" />Volver al Dashboard</Link></Button>
        } />
      </Card>
    </div>
  )
}
