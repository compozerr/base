import { createFileRoute } from '@tanstack/react-router'
import Navbar from '@/components/navbar'

export const Route = createFileRoute('/contact')({
  component: ContactPage,
})

function ContactPage() {
  // Replace this URL with your actual Tally form URL or other form service (Google Forms, Typeform, etc.)
  const formUrl = import.meta.env.VITE_CONTACT_FORM_URL || 'https://tally.so/embed/A7PoWo?alignLeft=1&hideTitle=1&transparentBackground=1'

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <div className="container mx-auto px-4 py-8 max-w-3xl">
        <div className="mb-6">
          <h1 className="text-3xl font-bold">Contact Us</h1>
          <p className="text-muted-foreground mt-2">
            Have a question or feedback? We'd love to hear from you.
          </p>
        </div>
        <div className="bg-white rounded-lg shadow-sm border overflow-hidden p-6">
          <iframe
            src={formUrl}
            loading="lazy" width="100%" height="379" title="Contact us"
          />
        </div>
      </div>
    </div>
  )
}
