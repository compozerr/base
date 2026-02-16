import { useRef } from "react"
import { motion, useInView } from "framer-motion"
import { Boxes, PuzzleIcon, Rocket, Database, Terminal, Globe, Shield, Zap } from "lucide-react"

const features = [
  {
    name: "Modular Templates",
    description:
      "Full-stack modules that combine frontend and backend. Authentication, database, payments — pick the building blocks you need.",
    icon: PuzzleIcon,
  },
  {
    name: "Community Marketplace",
    description: "Growing ecosystem of community-built modules. Share what you build, use what others have made.",
    icon: Boxes,
  },
  {
    name: "Instant Setup",
    description:
      "One command to scaffold. No boilerplate, no configuration marathons. Start writing business logic immediately.",
    icon: Rocket,
  },
  {
    name: "Pre-built Integrations",
    description:
      "Auth, database, Stripe, email, storage — battle-tested implementations that work together out of the box.",
    icon: Database,
  },
  {
    name: "n8n Hosting",
    description:
      "Deploy managed n8n automation instances with custom domains, SSL, and automatic backups. Independent infrastructure.",
    icon: Zap,
  },
  {
    name: "CLI-First Workflow",
    description:
      "Manage everything from your terminal. Create projects, deploy services, configure domains — all through a clean CLI.",
    icon: Terminal,
  },
  {
    name: "Custom Domains & SSL",
    description:
      "Bring your own domain with automatic SSL certificate provisioning. Zero-config HTTPS for every service.",
    icon: Globe,
  },
  {
    name: "Privacy-First Hosting",
    description:
      "European infrastructure, no Big Tech dependencies. Your data stays yours on independent, privacy-focused servers.",
    icon: Shield,
  },
]

function FeatureCard({ feature, index }: { feature: (typeof features)[0]; index: number }) {
  const ref = useRef<HTMLDivElement>(null)
  const isInView = useInView(ref, { once: true, margin: "-40px" })

  return (
    <motion.div
      ref={ref}
      initial={{ opacity: 0, y: 20 }}
      animate={isInView ? { opacity: 1, y: 0 } : {}}
      transition={{ duration: 0.4, delay: index * 0.05, ease: [0.25, 0.46, 0.45, 0.94] }}
      className="group relative rounded-xl border border-white/[0.06] bg-[hsl(var(--surface))] p-6 transition-all duration-300 hover:border-white/[0.1] hover:bg-[hsl(var(--surface-elevated))]"
    >
      {/* Hover glow */}
      <div className="pointer-events-none absolute inset-0 rounded-xl opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-gradient-to-br from-primary/[0.04] via-transparent to-transparent" />

      <div className="relative">
        <div className="mb-4 flex h-10 w-10 items-center justify-center rounded-lg bg-primary/[0.08] border border-primary/[0.12]">
          <feature.icon className="h-5 w-5 text-primary" />
        </div>
        <h3 className="mb-2 text-[15px] font-semibold tracking-tight">{feature.name}</h3>
        <p className="text-sm leading-relaxed text-muted-foreground">
          {feature.description}
        </p>
      </div>
    </motion.div>
  )
}

export default function Features() {
  const sectionRef = useRef<HTMLDivElement>(null)
  const isInView = useInView(sectionRef, { once: true, margin: "-100px" })

  return (
    <section className="relative py-24 md:py-32">
      <div className="container max-w-6xl">
        <div ref={sectionRef} className="mx-auto max-w-2xl text-center mb-16">
          <motion.h2
            initial={{ opacity: 0, y: 16 }}
            animate={isInView ? { opacity: 1, y: 0 } : {}}
            transition={{ duration: 0.5 }}
            className="text-3xl font-bold tracking-tight sm:text-4xl"
          >
            Everything you need to ship
          </motion.h2>
          <motion.p
            initial={{ opacity: 0, y: 16 }}
            animate={isInView ? { opacity: 1, y: 0 } : {}}
            transition={{ duration: 0.5, delay: 0.1 }}
            className="mt-4 text-muted-foreground text-base"
          >
            A complete platform for building and deploying full-stack applications.
            From templates to hosting, we've got every layer covered.
          </motion.p>
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {features.map((feature, i) => (
            <FeatureCard key={feature.name} feature={feature} index={i} />
          ))}
        </div>
      </div>
    </section>
  )
}
