import { createFileRoute } from '@tanstack/react-router'
import { useEffect, useCallback } from 'react'
import confetti from 'canvas-confetti'
import { Button } from '@/components/ui/button'
import { PartyPopper, Sparkles } from 'lucide-react'

export const Route = createFileRoute('/new-year')({
  component: NewYearComponent,
})

function NewYearComponent() {
  const fireConfetti = useCallback(() => {
    // Fire confetti from both sides
    const count = 200
    const defaults = {
      origin: { y: 0.7 },
      zIndex: 1000,
    }

    function fire(particleRatio: number, opts: confetti.Options) {
      confetti({
        ...defaults,
        ...opts,
        particleCount: Math.floor(count * particleRatio),
      })
    }

    // Left side burst
    fire(0.25, {
      spread: 26,
      startVelocity: 55,
      origin: { x: 0.2, y: 0.7 },
    })
    fire(0.2, {
      spread: 60,
      origin: { x: 0.2, y: 0.7 },
    })
    fire(0.35, {
      spread: 100,
      decay: 0.91,
      scalar: 0.8,
      origin: { x: 0.2, y: 0.7 },
    })
    fire(0.1, {
      spread: 120,
      startVelocity: 25,
      decay: 0.92,
      scalar: 1.2,
      origin: { x: 0.2, y: 0.7 },
    })
    fire(0.1, {
      spread: 120,
      startVelocity: 45,
      origin: { x: 0.2, y: 0.7 },
    })

    // Right side burst
    fire(0.25, {
      spread: 26,
      startVelocity: 55,
      origin: { x: 0.8, y: 0.7 },
    })
    fire(0.2, {
      spread: 60,
      origin: { x: 0.8, y: 0.7 },
    })
    fire(0.35, {
      spread: 100,
      decay: 0.91,
      scalar: 0.8,
      origin: { x: 0.8, y: 0.7 },
    })
    fire(0.1, {
      spread: 120,
      startVelocity: 25,
      decay: 0.92,
      scalar: 1.2,
      origin: { x: 0.8, y: 0.7 },
    })
    fire(0.1, {
      spread: 120,
      startVelocity: 45,
      origin: { x: 0.8, y: 0.7 },
    })
  }, [])

  // Fire confetti on page load
  useEffect(() => {
    // Small delay so the page renders first
    const timer = setTimeout(() => {
      fireConfetti()
    }, 300)

    return () => clearTimeout(timer)
  }, [fireConfetti])

  // Continuous subtle confetti falling
  useEffect(() => {
    const duration = 15 * 1000
    const animationEnd = Date.now() + duration
    const colors = ['#FFD700', '#FFA500', '#FF6347', '#87CEEB', '#98FB98', '#DDA0DD']

    const frame = () => {
      confetti({
        particleCount: 2,
        angle: 60,
        spread: 55,
        origin: { x: 0, y: 0 },
        colors: colors,
        zIndex: 1000,
      })
      confetti({
        particleCount: 2,
        angle: 120,
        spread: 55,
        origin: { x: 1, y: 0 },
        colors: colors,
        zIndex: 1000,
      })

      if (Date.now() < animationEnd) {
        requestAnimationFrame(frame)
      }
    }

    frame()
  }, [])

  return (
    <div className="relative min-h-screen flex flex-col items-center justify-center overflow-hidden">
      {/* Background gradients */}
      <div className="pointer-events-none fixed inset-0">
        <div className="absolute inset-0 bg-gradient-to-b from-background via-background/90 to-background" />
        <div className="absolute right-0 top-0 h-[500px] w-[500px] bg-yellow-500/20 blur-[100px]" />
        <div className="absolute bottom-0 left-0 h-[500px] w-[500px] bg-orange-500/20 blur-[100px]" />
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 h-[600px] w-[600px] bg-purple-500/10 blur-[120px]" />
      </div>

      {/* Content */}
      <div className="relative z-10 text-center space-y-8 px-4">
        <div className="flex justify-center gap-4 mb-6">
          <Sparkles className="h-12 w-12 text-yellow-400 animate-pulse" />
          <PartyPopper className="h-12 w-12 text-orange-400 animate-bounce" />
          <Sparkles className="h-12 w-12 text-yellow-400 animate-pulse" />
        </div>

        <h1 className="text-6xl md:text-8xl font-bold bg-gradient-to-r from-yellow-400 via-orange-500 to-pink-500 bg-clip-text text-transparent animate-pulse">
          Happy New Year!
        </h1>

        <p className="text-2xl md:text-3xl text-muted-foreground font-light">
          2026
        </p>

        <p className="text-lg md:text-xl text-muted-foreground max-w-md mx-auto">
          Wishing you a year filled with joy, success, and endless possibilities!
        </p>

        <Button
          size="lg"
          onClick={fireConfetti}
          className="bg-gradient-to-r from-yellow-500 to-orange-500 hover:from-yellow-600 hover:to-orange-600 text-white font-semibold text-lg px-8 py-6"
        >
          <PartyPopper className="mr-2 h-5 w-5" />
          More Confetti!
        </Button>
      </div>

      {/* Decorative elements */}
      <div className="absolute bottom-8 left-1/2 -translate-x-1/2 text-muted-foreground/50 text-sm">
        Click the button for more celebration!
      </div>
    </div>
  )
}
