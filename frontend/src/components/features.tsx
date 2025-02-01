import { useRef, useState } from "react"
import { motion, useMotionTemplate, useMotionValue, animate, useSpring } from "framer-motion"
import { Boxes, PuzzleIcon, Rocket, Database } from "lucide-react"
import { Button } from "@/components/ui/button"

const features = [
  {
    name: "Modular Full-Stack Templates",
    description:
      "Build your project faster with ready-to-use full-stack modules combining frontend and backend implementations.",
    icon: PuzzleIcon,
  },
  {
    name: "Community Marketplace",
    description: "Access a growing ecosystem of community-built modules, from authentication to payment systems.",
    icon: Boxes,
  },
  {
    name: "Instant Project Setup",
    description:
      "Start your project in minutes by selecting and combining the modules you need, no configuration hassle.",
    icon: Rocket,
  },
  {
    name: "Pre-built Integrations",
    description:
      "Choose from various integrated solutions including auth, database, Stripe, and more - all working seamlessly together.",
    icon: Database,
  },
]

function FeatureCard({ feature }: { feature: (typeof features)[0] }) {
  const ref = useRef<HTMLDivElement>(null)
  const [hovered, setHovered] = useState(false)

  // Mouse position for gradient and rotation
  const mouseX = useMotionValue(0)
  const mouseY = useMotionValue(0)

  // Mouse position for rotation (with spring physics)
  const rotateX = useSpring(0, { stiffness: 150, damping: 15 })
  const rotateY = useSpring(0, { stiffness: 150, damping: 15 })

  // Click animation state
  const [isPressed, setIsPressed] = useState(false)

  function onMouseMove({ currentTarget, clientX, clientY }: React.MouseEvent) {
    const { left, top, width, height } = currentTarget.getBoundingClientRect()

    // Calculate mouse position for gradient
    const x = (clientX - left) / width
    const y = (clientY - top) / height
    mouseX.set(x)
    mouseY.set(y)

    // Calculate rotation based on mouse position
    const rotateXValue = (y - 0.5) * -20
    const rotateYValue = (x - 0.5) * 20

    rotateX.set(rotateXValue)
    rotateY.set(rotateYValue)
  }

  function onClick() {
    setIsPressed(true)
    // Exaggerate the current rotation
    const currentX = rotateX.get()
    const currentY = rotateY.get()

    // Animate to an exaggerated rotation and back
    animate(rotateX, [currentX * 2, currentX], {
      type: "spring",
      stiffness: 400,
      damping: 10,
    })
    animate(rotateY, [currentY * 2, currentY], {
      type: "spring",
      stiffness: 400,
      damping: 10,
    })

    setTimeout(() => setIsPressed(false), 150)
  }

  const background = useMotionTemplate`
    radial-gradient(
      circle at ${mouseX.get() * 100}% ${mouseY.get() * 100}%,
      rgba(var(--primary-rgb) / 0.1) 0%,
      rgba(var(--primary-rgb) / 0.1) 25%,
      rgba(var(--primary-rgb) / 0.05) 50%,
      transparent 100%
    ),
    linear-gradient(
      to bottom right,
      rgba(var(--primary-rgb) / ${isPressed ? 0.2 : 0.1}) 0%,
      rgba(var(--primary-rgb) / 0.05) 25%,
      rgba(var(--primary-rgb) / 0.05) 50%,
      transparent 100%
    )
  `

  return (
    <motion.div
      ref={ref}
      onMouseMove={onMouseMove}
      onClick={onClick}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => {
        setHovered(false)
        rotateX.set(0)
        rotateY.set(0)
      }}
      style={{
        transformStyle: "preserve-3d",
        rotateX,
        rotateY,
      }}
      className="group relative overflow-hidden rounded-lg border bg-background p-8 transition-all duration-200"
      initial={{ scale: 1 }}
      whileHover={{ scale: 1.02, transition: { duration: 0.2 } }}
    >
      <motion.div
        className="pointer-events-none absolute inset-0 opacity-0 transition-opacity duration-300 group-hover:opacity-100"
        style={{ background }}
      />
      <div className="relative flex items-center gap-4" style={{ transform: "translateZ(20px)" }}>
        <feature.icon className="h-8 w-8" />
        <h3 className="font-bold">{feature.name}</h3>
      </div>
      <p className="relative mt-2 text-muted-foreground" style={{ transform: "translateZ(20px)" }}>
        {feature.description}
      </p>

      <motion.div
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: hovered ? 1 : 0, y: hovered ? 0 : 10 }}
        transition={{ duration: 0.2 }}
        className="relative mt-4"
        style={{ transform: "translateZ(20px)" }}
      >
        <Button variant="secondary" className="w-full">
          Learn More
        </Button>
      </motion.div>
    </motion.div>
  )
}

export default function Features() {
  return (
    <section className="container space-y-16 py-24 md:py-32">
      <div className="mx-auto max-w-[58rem] text-center">
        <h2 className="font-bold text-3xl leading-[1.1] sm:text-3xl md:text-5xl">Build Faster, Build Better</h2>
        <p className="mt-4 text-muted-foreground sm:text-lg">
          Choose the modules you need and start building your full-stack application in minutes.
        </p>
      </div>
      <div className="mx-auto grid max-w-5xl grid-cols-1 gap-8 md:grid-cols-2" style={{ perspective: "1000px" }}>
        {features.map((feature) => (
          <FeatureCard key={feature.name} feature={feature} />
        ))}
      </div>
    </section>
  )
}

