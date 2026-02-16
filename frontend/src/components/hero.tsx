import { Button } from "@/components/ui/button"
import { getLink } from "@/links"
import { ArrowRight, FileText } from "lucide-react"
import { CopyCommand } from "./copy-command"

export default function Hero() {
  return (
    <section className="relative overflow-hidden">
      {/* Subtle grid background */}
      <div className="absolute inset-0 bg-grid-pattern mask-fade-out opacity-40" />

      {/* Glow accent */}
      <div className="pointer-events-none absolute left-1/2 top-0 -translate-x-1/2 h-[500px] w-[800px] bg-primary/[0.07] blur-[120px] rounded-full" />

      <div className="container relative max-w-6xl flex flex-col items-center justify-center min-h-[calc(100vh-4rem)] py-24 text-center">
        {/* Eyebrow */}
        <div
          className="animate-fade-up mb-6 inline-flex items-center gap-2 rounded-full border border-primary/20 bg-primary/[0.06] px-4 py-1.5 text-xs font-medium text-primary"
          style={{ animationDelay: '0ms' }}
        >
          <span className="relative flex h-1.5 w-1.5">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-primary opacity-75" />
            <span className="relative inline-flex h-1.5 w-1.5 rounded-full bg-primary" />
          </span>
          Built for developers, by developers
        </div>

        {/* Headline */}
        <h1
          className="animate-fade-up max-w-4xl text-4xl font-bold tracking-tight sm:text-5xl md:text-6xl lg:text-7xl"
          style={{ animationDelay: '100ms' }}
        >
          Ship your stack{' '}
          <span className="bg-gradient-to-r from-primary via-blue-400 to-cyan-400 bg-clip-text text-transparent">
            faster
          </span>
        </h1>

        {/* Subheadline */}
        <p
          className="animate-fade-up mt-6 max-w-2xl text-base text-muted-foreground sm:text-lg leading-relaxed"
          style={{ animationDelay: '200ms' }}
        >
          Modular full-stack templates with production-ready auth, database, payments, and more.
          Pick what you need, skip what you don't. Deploy in minutes.
        </p>

        {/* Install command */}
        <div className="animate-fade-up mt-10 w-full max-w-xl" style={{ animationDelay: '300ms' }}>
          <CopyCommand />
        </div>

        {/* CTAs */}
        <div
          className="animate-fade-up mt-8 flex flex-col sm:flex-row items-center gap-3"
          style={{ animationDelay: '400ms' }}
        >
          <Button
            onClick={() => open(getLink("documentation"), "_blank")}
            size="lg"
            className="gap-2 bg-primary hover:bg-primary/90 text-primary-foreground font-medium px-6 h-11"
          >
            <FileText className="h-4 w-4" />
            Documentation
          </Button>
          <Button
            variant="ghost"
            size="lg"
            className="gap-2 text-muted-foreground hover:text-foreground font-medium px-6 h-11"
            onClick={() => window.location.href = '/pricing'}
          >
            View pricing
            <ArrowRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </section>
  )
}
