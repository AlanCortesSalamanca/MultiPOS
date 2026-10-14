import type { ReactNode } from 'react'

type PageHeadingProps = {
  title: string
  description: string
  actions?: ReactNode
}

export function PageHeading({ title, description, actions }: PageHeadingProps) {
  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
      <div className="max-w-2xl space-y-2">
        <p className="text-xs font-semibold tracking-widest text-primary uppercase">Administración y logística</p>
        <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">{title}</h1>
        <p className="text-sm leading-relaxed text-muted-foreground sm:text-base">{description}</p>
      </div>
      {actions && <div className="flex shrink-0 flex-wrap gap-2">{actions}</div>}
    </div>
  )
}
