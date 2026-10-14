import { ChevronRight, Menu } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { SheetTrigger } from '@/components/ui/sheet'

export function Header({ section }: { section: string }) {
  return (
    <header className="sticky top-0 z-20 flex min-h-20 items-center justify-between gap-3 border-b bg-card px-4 sm:px-6 lg:px-8">
      <div className="flex min-w-0 items-center gap-3">
        <SheetTrigger asChild>
          <Button variant="ghost" size="icon" className="lg:hidden" aria-label="Abrir navegación">
            <Menu aria-hidden="true" />
          </Button>
        </SheetTrigger>
        <div className="flex min-w-0 items-center gap-2 text-sm">
          <span className="hidden text-muted-foreground sm:inline">MultiPOS</span>
          <ChevronRight className="hidden size-4 text-muted-foreground sm:inline" aria-hidden="true" />
          <span className="truncate font-medium">{section}</span>
        </div>
      </div>
      <Badge variant="warning" className="shrink-0">Sin datos reales</Badge>
    </header>
  )
}
