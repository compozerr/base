import { createFileRoute } from '@tanstack/react-router'
import Navbar from '@/components/navbar'
import Footer from '@/components/footer'

export const Route = createFileRoute('/contact')({
  component: ContactPage,
})

function ContactPage() {
  const formUrl = import.meta.env.VITE_CONTACT_FORM_URL || 'https://tally.so/embed/A7PoWo?alignLeft=1&hideTitle=1&transparentBackground=1'

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <div className="container max-w-3xl py-16 md:py-24">
        <div className="mb-8">
          <div className="animate-fade-up mb-4 inline-flex items-center gap-2 rounded-full border border-primary/20 bg-primary/[0.06] px-4 py-1.5 text-xs font-medium text-primary">
            Get in touch
          </div>
          <h1 className="animate-fade-up text-3xl font-bold tracking-tight md:text-4xl" style={{ animationDelay: '100ms' }}>
            Contact us
          </h1>
          <p className="animate-fade-up mt-3 text-muted-foreground" style={{ animationDelay: '200ms' }}>
            Have a question or feedback? We'd love to hear from you.
          </p>
        </div>

        <div className="animate-fade-up rounded-2xl border border-white/[0.06] bg-[hsl(var(--surface))] overflow-hidden p-6" style={{ animationDelay: '300ms' }}>
          <iframe
            src={formUrl}
            loading="lazy"
            width="100%"
            height="379"
            title="Contact us"
            className="border-0"
          />
        </div>
      </div>

      <Footer />
    </div>
  )
}
