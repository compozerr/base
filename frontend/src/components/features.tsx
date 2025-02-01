import { Boxes, PuzzleIcon, Rocket, Database } from "lucide-react"

const features = [
  {
    name: "Modular Full-Stack Templates",
    description: "Build your project faster with ready-to-use full-stack modules combining frontend and backend implementations.",
    icon: PuzzleIcon,
  },
  {
    name: "Community Marketplace",
    description: "Access a growing ecosystem of community-built modules, from authentication to payment systems.",
    icon: Boxes,
  },
  {
    name: "Instant Project Setup",
    description: "Start your project in minutes by selecting and combining the modules you need, no configuration hassle.",
    icon: Rocket,
  },
  {
    name: "Pre-built Integrations",
    description: "Choose from various integrated solutions including auth, database, Stripe, and more - all working seamlessly together.",
    icon: Database,
  },
]

// Rest of the component remains the same, just update the title and subtitle
export default function Features() {
  return (
    <section className="container space-y-16 py-24 md:py-32">
      <div className="mx-auto max-w-[58rem] text-center">
        <h2 className="font-bold text-3xl leading-[1.1] sm:text-3xl md:text-5xl">Build Faster, Build Better</h2>
        <p className="mt-4 text-muted-foreground sm:text-lg">
          Choose the modules you need and start building your full-stack application in minutes.
        </p>
      </div>
      <div className="mx-auto grid max-w-5xl grid-cols-1 gap-8 md:grid-cols-2">
        {features.map((feature) => (
          <div key={feature.name} className="relative overflow-hidden rounded-lg border bg-background p-8">
            <div className="flex items-center gap-4">
              <feature.icon className="h-8 w-8" />
              <h3 className="font-bold">{feature.name}</h3>
            </div>
            <p className="mt-2 text-muted-foreground">{feature.description}</p>
          </div>
        ))}
      </div>
    </section>
  )
}

