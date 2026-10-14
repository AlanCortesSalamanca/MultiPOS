import { useEffect, useRef, useState } from 'react'
import { Info } from 'lucide-react'
import { Outlet, useLocation } from 'react-router'
import { navigationItems } from '@/app/navigation'
import { Header } from '@/components/layout/header'
import { SidebarContent } from '@/components/layout/sidebar'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/components/ui/sheet'

export function AdminLayout() {
  const { pathname } = useLocation()
  const [menuOpen, setMenuOpen] = useState(false)
  const mainRef = useRef<HTMLElement>(null)
  const previousPath = useRef(pathname)
  const focusContentAfterClose = useRef(false)
  const normalizedPath = pathname.replace(/\/+$/, '') || '/'
  const section = navigationItems.find((item) => item.href === normalizedPath)?.label ?? 'Página no encontrada'

  useEffect(() => {
    document.title = `${section} · MultiPOS`
    if (previousPath.current !== pathname) {
      previousPath.current = pathname
      focusContentAfterClose.current = true
      setMenuOpen(false)
      mainRef.current?.focus({ preventScroll: true })
      window.scrollTo(0, 0)
    }
  }, [pathname, section])

  useEffect(() => {
    const desktop = window.matchMedia('(min-width: 64rem)')
    const closeOnDesktop = () => {
      if (desktop.matches) {
        focusContentAfterClose.current = true
        setMenuOpen(false)
      }
    }
    desktop.addEventListener('change', closeOnDesktop)
    return () => desktop.removeEventListener('change', closeOnDesktop)
  }, [])

  return (
    <Sheet open={menuOpen} onOpenChange={(open) => {
      focusContentAfterClose.current = false
      setMenuOpen(open)
    }}>
      <div className="min-h-dvh lg:pl-64">
        <a href="#contenido-principal" className="fixed top-3 left-3 z-[60] -translate-y-24 rounded-md bg-primary px-4 py-3 text-sm font-medium text-primary-foreground focus:translate-y-0">
          Saltar al contenido
        </a>

        <aside aria-label="Menú administrativo" className="fixed inset-y-0 left-0 hidden w-64 lg:block">
          <SidebarContent />
        </aside>

        <Header section={section} />

        <main id="contenido-principal" ref={mainRef} tabIndex={-1} className="mx-auto max-w-7xl space-y-6 px-4 py-6 outline-none sm:px-6 sm:py-8 lg:px-8">
          <aside aria-label="Estado de implementación" className="flex items-start gap-3 rounded-lg border bg-card p-4">
            <Info className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
            <p className="text-sm leading-relaxed text-muted-foreground">
              <strong className="font-medium text-foreground">Interfaz inicial.</strong> Todavía no hay conexión a servicios de negocio. No se consultan ni guardan datos reales.
            </p>
          </aside>
          <Outlet />
          <footer className="border-t pt-5 text-xs leading-relaxed text-muted-foreground">
            MultiPOS · Web de administración y logística · FRONT-01
          </footer>
        </main>
      </div>

      <SheetContent className="bg-sidebar" onCloseAutoFocus={(event) => {
        if (focusContentAfterClose.current) {
          event.preventDefault()
          mainRef.current?.focus({ preventScroll: true })
          focusContentAfterClose.current = false
        }
      }}>
        <SheetTitle className="sr-only">Navegación administrativa</SheetTitle>
        <SheetDescription className="sr-only">Elige Dashboard, Productos, Inventario o Reportes.</SheetDescription>
        <SidebarContent onNavigate={() => {
          focusContentAfterClose.current = true
          setMenuOpen(false)
        }} />
      </SheetContent>
    </Sheet>
  )
}
