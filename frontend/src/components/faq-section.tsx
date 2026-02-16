import { useState } from 'react'
import { Helmet } from 'react-helmet-async'
import { ChevronDown } from 'lucide-react'

export interface FAQItem {
  question: string
  answer: string
}

interface FAQSectionProps {
  items?: FAQItem[]
  title?: string
  className?: string
}

const defaultFAQItems: FAQItem[] = [
  {
    question: 'Can I upgrade or downgrade my tier?',
    answer:
      'Yes! You can change your server tier at any time from your project settings. Your service will be migrated seamlessly.',
  },
  {
    question: "What's included in all tiers?",
    answer:
      'All tiers include n8n hosting, custom domain support, SSL certificates, automatic backups, and 24/7 infrastructure monitoring. You get the full compozerr experience regardless of tier.',
  },
  {
    question: 'How does the promotional pricing work?',
    answer:
      'The T1 tier is specially priced at $5/month (37% off from $8/month). This pricing is automatically applied when you create an n8n project on the T1 tier.',
  },
  {
    question: 'Do you offer refunds?',
    answer:
      'Refunds are handled on a case-by-case basis. Please contact us through our contact page if you have any billing concerns.',
  },
]

function FAQItemComponent({ item }: { item: FAQItem }) {
  const [open, setOpen] = useState(false)

  return (
    <div className="border-b border-white/[0.04] last:border-0">
      <button
        onClick={() => setOpen(!open)}
        className="flex w-full items-center justify-between py-5 text-left transition-colors hover:text-foreground"
      >
        <span className="text-sm font-medium pr-4">{item.question}</span>
        <ChevronDown
          className={`h-4 w-4 flex-shrink-0 text-muted-foreground transition-transform duration-200 ${
            open ? 'rotate-180' : ''
          }`}
        />
      </button>
      <div
        className={`overflow-hidden transition-all duration-200 ${
          open ? 'max-h-40 pb-5' : 'max-h-0'
        }`}
      >
        <p className="text-sm leading-relaxed text-muted-foreground">{item.answer}</p>
      </div>
    </div>
  )
}

export default function FAQSection({
  items = defaultFAQItems,
  title = 'Frequently asked questions',
  className = '',
}: FAQSectionProps) {
  return (
    <>
      <Helmet>
        <script type="application/ld+json">
          {JSON.stringify({
            "@context": "https://schema.org",
            "@type": "FAQPage",
            "mainEntity": items.map(item => ({
              "@type": "Question",
              "name": item.question,
              "acceptedAnswer": {
                "@type": "Answer",
                "text": item.answer
              }
            }))
          })}
        </script>
      </Helmet>
      <div className={className}>
        <h2 className="text-xl font-semibold mb-6">{title}</h2>
        <div className="rounded-2xl border border-white/[0.06] bg-[hsl(var(--surface))] px-6">
          {items.map((item, idx) => (
            <FAQItemComponent key={idx} item={item} />
          ))}
        </div>
      </div>
    </>
  )
}
