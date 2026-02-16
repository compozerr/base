import { cn } from '@/lib/utils'

interface CompozerrLogoProps {
  className?: string
}

export function CompozerrLogo({ className }: CompozerrLogoProps) {
  return (
    <span className={cn('font-semibold tracking-tight', className)}>
      compozerr
    </span>
  )
}
