import { PanelsTopLeft } from 'lucide-react'
import { NavLink } from 'react-router'
import { navigationItems } from '@/app/navigation'
import { cn } from '@/lib/utils'

export function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  return (
    <div className="flex h-full min-h-0 flex-col bg-sidebar text-sidebar-foreground">
      <div className="flex items-center gap-3 border-b border-white/10 px-6 py-6">
        <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-teal-400/15 text-teal-300">
          <PanelsTopLeft className="size-5" aria-hidden="true" />
        </div>
        <div>
          <p className="text-lg font-semibold tracking-tight text-white">MultiPOS</p>
          <p className="text-xs text-slate-400">Web administrativa</p>
        </div>
      </div>

      <nav aria-label="Navegación principal" className="flex-1 overflow-y-auto px-4 py-7">
        <p className="mb-3 px-3 text-xs font-medium tracking-wider text-slate-400 uppercase">Espacio de trabajo</p>
        <ul className="space-y-1.5">
          {navigationItems.map(({ href, label, icon: Icon }) => (
            <li key={href}>
              <NavLink
                to={href}
                end
                onClick={onNavigate}
                className={({ isActive }) => cn(
                  'flex min-h-11 items-center gap-3 rounded-lg px-3 py-3 text-sm transition-colors motion-reduce:transition-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white',
                  isActive
                    ? 'bg-sidebar-accent font-medium text-white'
                    : 'text-slate-300 hover:bg-white/5 hover:text-white',
                )}
              >
                <Icon className="size-5 shrink-0" aria-hidden="true" />
                {label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      <div className="m-4 rounded-lg border border-white/10 p-4">
        <p className="text-xs font-semibold text-slate-200">FRONT-01 · Interfaz inicial</p>
        <p className="mt-2 text-xs leading-relaxed text-slate-400">Administración y logística. La operación POS pertenece a la aplicación Windows independiente.</p>
      </div>
    </div>
  )
}
