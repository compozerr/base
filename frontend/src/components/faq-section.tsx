import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

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
    question: 'How does the Black Friday special work?',
    answer:
      'The T1 tier is specially priced at $5/month (37% off from $8/month) for Black Friday. This pricing is automatically applied when you create an n8n project on the T1 tier.',
  },
  {
    question: 'Do you offer refunds?',
    answer:
      'Refunds are handled on a case-by-case basis. Please contact us through our contact page if you have any billing concerns.',
  },
]

export default function FAQSection({
  items = defaultFAQItems,
  title = 'Frequently Asked Questions',
  className = '',
}: FAQSectionProps) {
  return (
    <Card className={`border-zinc-800 bg-zinc-900/50 backdrop-blur-sm ${className}`}>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        {items.map((item, idx) => (
          <div key={idx}>
            <h3 className="font-semibold mb-2">{item.question}</h3>
            <p className="text-sm text-muted-foreground">{item.answer}</p>
          </div>
        ))}
      </CardContent>
    </Card>
  )
}
