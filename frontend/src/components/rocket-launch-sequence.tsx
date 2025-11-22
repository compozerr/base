import { useState, useEffect, useRef } from 'react'
import { Canvas, useFrame, useThree } from '@react-three/fiber'
import { PerspectiveCamera } from '@react-three/drei'
import { motion } from 'framer-motion'
import { RocketModel } from './rocket-model'
import { ParticleSystem } from './particle-system'
import * as THREE from 'three'

interface RocketLaunchSequenceProps {
  onComplete: () => void
  projectId?: string | null
  startFromBottom?: boolean
  onIdleReached?: () => void // Called when rocket reaches center and is idle
}

// Smooth Camera Controller
function CameraController() {
  const { camera } = useThree()

  useFrame(() => {
    camera.position.set(0, 3, 8)
    camera.lookAt(0, 0, 0)
  })

  return null
}

export function RocketLaunchSequence({
  onComplete,
  projectId,
  startFromBottom = false,
  onIdleReached
}: RocketLaunchSequenceProps) {
  // If not starting from bottom, we're continuing from a previous page - start at idle
  const [rocketY, setRocketY] = useState(startFromBottom ? -8 : 0)
  const [engineGlow, setEngineGlow] = useState(startFromBottom ? 0 : 8)
  const [showConfetti, setShowConfetti] = useState(false)
  const [sceneOpacity, setSceneOpacity] = useState(startFromBottom ? 0 : 1)
  const [phase, setPhase] = useState<'rising' | 'idle' | 'launching' | 'complete'>(
    startFromBottom ? 'rising' : 'idle'
  )

  useEffect(() => {
    const timeline = async () => {
      if (startFromBottom) {
        // Coming from n8n page - do the full sequence
        // Phase 0: Fade in stars/scene immediately
        smoothTransition(600, (progress) => {
          setSceneOpacity(progress)
        })

        // Phase 1: Rise to center from bottom
        await smoothTransition(1000, (progress) => {
          setRocketY(-8 + progress * 8) // -8 to 0 (center)
        })

        // Phase 2: Engine ignition at center
        await smoothTransition(800, (progress) => {
          setEngineGlow(progress * 8)
        })

        // Phase 3: IDLE at center - wait for page load
        setPhase('idle')
        setEngineGlow(8) // Keep steady glow

        // Notify parent that we're at idle (page can navigate now)
        if (onIdleReached) {
          onIdleReached()
        }
      } else {
        // Already on /projects page - start at idle and wait for continue call
        setPhase('idle')
        if (onIdleReached) {
          onIdleReached()
        }
      }
    }

    timeline()
  }, [onIdleReached, startFromBottom])

  // External trigger to continue from idle to launch
  const continueToLaunch = async () => {
    if (phase !== 'idle') return

    setPhase('launching')

    // Phase 4: Launch from center to top
    await smoothTransition(1500, (progress) => {
      const eased = easeInOutCubic(progress)
      setRocketY(eased * 15) // 0 to 15 (top)
      setEngineGlow(8 + eased * 7)
    })

    // Phase 5: CONFETTI at the TOP (fires once!)
    setShowConfetti(true)
    await delay(800)

    // Phase 6: Fade out
    setPhase('complete')
    await delay(500)

    onComplete()
  }

  // Store the continue function in a ref so it can be called externally
  const continueRef = useRef(continueToLaunch)
  continueRef.current = continueToLaunch

  // Expose continue function to parent
  useEffect(() => {
    // @ts-ignore - Attach to window
    window.__rocketContinue = () => continueRef.current()

    // Cleanup on unmount - but only if we're in complete phase
    return () => {
      if (phase === 'complete') {
        // @ts-ignore
        delete window.__rocketContinue
      }
      // Otherwise leave it for the next page to use
    }
  }, [phase])

  return (
    <div className="fixed inset-0 z-50 bg-black">
      {/* Three.js Canvas */}
      <Canvas>
        <PerspectiveCamera makeDefault position={[0, 3, 8]} fov={60} />
        <CameraController />

        {/* Soft Lighting */}
        <ambientLight intensity={0.4} />
        <pointLight position={[3, 5, 3]} intensity={0.3} />

        {/* Rocket */}
        <RocketModel
          position={[0, rocketY, 0]}
          vibrationIntensity={0}
          engineGlow={engineGlow}
        />

        {/* Confetti Effects - only fires ONCE at the TOP! */}
        {showConfetti && (
          <ParticleSystem
            isActive={true}
            intensity={1}
            type="confetti"
            position={[0, rocketY, 0]}
          />
        )}

        {/* Minimal star background - fades in */}
        {Array.from({ length: 100 }).map((_, i) => (
          <mesh
            key={i}
            position={[
              (Math.random() - 0.5) * 40,
              Math.random() * 20 + 5,
              (Math.random() - 0.5) * 40
            ]}
          >
            <sphereGeometry args={[0.03, 4, 4]} />
            <meshBasicMaterial color="white" opacity={0.6 * sceneOpacity} transparent />
          </mesh>
        ))}
      </Canvas>

      {/* Smooth fade out overlay - fades TO WHITE to reveal page underneath */}
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: phase === 'complete' ? 1 : 0 }}
        transition={{ duration: 0.8, ease: 'easeInOut' }}
        className="absolute inset-0 bg-background pointer-events-none"
      />
    </div>
  )
}

// Helper functions
function delay(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms))
}

function smoothTransition(duration: number, callback: (progress: number) => void): Promise<void> {
  return new Promise(resolve => {
    const startTime = Date.now()

    const animate = () => {
      const elapsed = Date.now() - startTime
      const progress = Math.min(elapsed / duration, 1)
      const eased = easeInOutCubic(progress)

      callback(eased)

      if (progress < 1) {
        requestAnimationFrame(animate)
      } else {
        resolve()
      }
    }

    animate()
  })
}

function easeInOutCubic(t: number): number {
  return t < 0.5
    ? 4 * t * t * t
    : 1 - Math.pow(-2 * t + 2, 3) / 2
}
