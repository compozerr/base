import { CopyButton } from "./copy-button"

export function CopyCommand() {
  const command = `/bin/bash -c "$(curl -fsSL https://compozerr.com/install.sh)"`

  return (
    <div className="mx-auto max-w-2xl">
      <div className="relative flex items-center rounded-xl border border-white/[0.06] bg-[hsl(var(--surface))] px-5 py-3.5 font-mono text-[13px]">
        <span className="mr-3 select-none text-primary/60">$</span>
        <span className="flex-1 truncate text-foreground/80">{command}</span>
        <CopyButton value={command} className="ml-3 flex-shrink-0" />
      </div>
    </div>
  )
}
