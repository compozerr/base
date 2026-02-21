import { createFileRoute } from '@tanstack/react-router'
import Hero from '@/components/hero'
import Features from '@/components/features'
import Footer from '@/components/footer'
import Navbar from '@/components/navbar'
import SEO from '@/components/seo'

export const Route = createFileRoute('/')({
  component: HomeComponent,
})

function HomeComponent() {
  return (
    <div className="relative min-h-screen bg-background">
      <SEO
        canonical="/"
        structuredData={{
          "@context": "https://schema.org",
          "@type": "Organization",
          "name": "compozerr",
          "url": "https://compozerr.com",
          "description": "Modular full-stack templates with production-ready auth, database, payments, and more.",
          "sameAs": []
        }}
      />
      <Navbar />
      <Hero />
      <Features />

      {/* Social proof / trust section */}
      <section className="border-t border-white/[0.06] py-12 overflow-hidden">
        <p className="text-sm font-medium text-muted-foreground tracking-wide uppercase text-center mb-6">
          Powered by independent infrastructure
        </p>
        <div className="relative">
          {/* Fade edges */}
          <div className="pointer-events-none absolute left-0 top-0 bottom-0 w-32 z-10 bg-gradient-to-r from-background to-transparent" />
          <div className="pointer-events-none absolute right-0 top-0 bottom-0 w-32 z-10 bg-gradient-to-l from-background to-transparent" />
          <div className="flex animate-marquee">
            {[...Array(4)].map((_, setIndex) => (
              <div key={setIndex} className="flex shrink-0 items-center gap-16 px-8">
                {['.NET 9', 'React 19', 'PostgreSQL', 'Docker', 'Traefik', 'OVHcloud'].map((tech) => (
                  <span key={`${setIndex}-${tech}`} className="text-lg font-semibold tracking-tight text-muted-foreground/40 whitespace-nowrap">
                    {tech}
                  </span>
                ))}
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA section */}
      <section className="py-24">
        <div className="container max-w-6xl">
          <div className="relative overflow-hidden rounded-2xl border border-white/[0.06] bg-[hsl(var(--surface))] px-8 py-16 text-center sm:px-16">
            {/* Background accent */}
            <div className="pointer-events-none absolute inset-0 bg-gradient-to-br from-primary/[0.06] via-transparent to-transparent" />
            <div className="relative">
              <h2 className="text-3xl font-bold tracking-tight sm:text-4xl">
                Ready to build?
              </h2>
              <p className="mx-auto mt-4 max-w-lg text-muted-foreground">
                Get started in under a minute. Install the CLI, pick your modules, and ship.
              </p>
              <div className="mt-8 flex flex-col sm:flex-row items-center justify-center gap-3">
                <a
                  href="/n8n"
                  className="inline-flex h-11 items-center justify-center gap-2 rounded-lg bg-primary px-6 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
                >
                  Deploy n8n instance
                </a>
                <a
                  href="/pricing"
                  className="inline-flex h-11 items-center justify-center gap-2 rounded-lg border border-white/[0.1] bg-transparent px-6 text-sm font-medium text-foreground transition-colors hover:bg-white/[0.04]"
                >
                  View pricing
                </a>
              </div>
            </div>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  )
}
