import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

type EmptyStateProps = {
  icon: LucideIcon
  title: string
  description: string
  action?: ReactNode
}

export function EmptyState({ icon: Icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center px-4 py-12 text-center sm:py-16">
      <div className="mb-5 flex size-14 items-center justify-center rounded-2xl bg-secondary text-muted-foreground">
        <Icon className="size-6" aria-hidden="true" />
      </div>
      <p className="font-semibold">{title}</p>
      <p className="mt-2 max-w-md text-sm leading-relaxed text-muted-foreground">{description}</p>
      {action && <div className="mt-6">{action}</div>}
    </div>
  )
}
