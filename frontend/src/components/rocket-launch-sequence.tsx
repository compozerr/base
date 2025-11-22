import { useState, useEffect, useRef } from 'react'
import { Canvas, useFrame, useThree } from '@react-three/fiber'
import { PerspectiveCamera } from '@react-three/drei'
import { motion, AnimatePresence } from 'framer-motion'
import { RocketModel } from './rocket-model'
import { ParticleSystem } from './particle-system'
import * as THREE from 'three'

interface RocketLaunchSequenceProps {
  onComplete: () => void
  projectId?: string | null
}

// Camera animation controller
function CameraController({ phase }: { phase: number }) {
  const { camera } = useThree()

  useFrame(() => {
    if (phase >= 3 && phase < 5) {
      // Phase 3-5: Pan down to rocket (from high to medium view)
      const progress = Math.min((phase - 3) / 2, 1)
      camera.position.y = THREE.MathUtils.lerp(10, 4, progress)
      camera.position.z = THREE.MathUtils.lerp(15, 10, progress)
      camera.lookAt(0, 2, 0)
    } else if (phase >= 5 && phase < 8) {
      // Phase 5-8: Countdown - zoom closer
      const progress = Math.min((phase - 5) / 3, 1)
      camera.position.y = THREE.MathUtils.lerp(4, 3, progress)
      camera.position.z = THREE.MathUtils.lerp(10, 7, progress)
      camera.lookAt(0, 2, 0)
    } else if (phase >= 10) {
      // Phase 10+: Follow rocket upward
      const progress = Math.min((phase - 10) / 3, 1)
      const rocketY = progress * 20 // Rocket rises
      camera.position.y = THREE.MathUtils.lerp(3, 15 + rocketY, progress)
      camera.position.z = THREE.MathUtils.lerp(7, 10, progress)
      camera.lookAt(0, rocketY, 0)
    }
  })

  return null
}

export function RocketLaunchSequence({ onComplete, projectId }: RocketLaunchSequenceProps) {
  const [phase, setPhase] = useState(0)
  const [countdown, setCountdown] = useState(10)
  const [rocketPosition, setRocketPosition] = useState<[number, number, number]>([0, 0, 0])
  const [isLaunching, setIsLaunching] = useState(false)
  const [particleIntensity, setParticleIntensity] = useState(0)
  const [shakeIntensity, setShakeIntensity] = useState(0)

  const audioRefs = useRef<{
    countdown?: HTMLAudioElement
    ignition?: HTMLAudioElement
    launch?: HTMLAudioElement
    fly?: HTMLAudioElement
  }>({})

  // Preload and setup sounds
  useEffect(() => {
    // Note: You'll need to add these sound files to public/sounds/
    // For now, we'll create placeholder Audio objects
    // audioRefs.current.countdown = new Audio('/sounds/countdown-beep.mp3')
    // audioRefs.current.ignition = new Audio('/sounds/rocket-ignition.mp3')
    // audioRefs.current.launch = new Audio('/sounds/rocket-launch.mp3')
    // audioRefs.current.fly = new Audio('/sounds/rocket-fly.mp3')
  }, [])

  // Animation timeline
  useEffect(() => {
    const timeline = [
      { time: 0, action: () => setPhase(0) }, // Fade in "Launch in T-10 seconds"
      { time: 2000, action: () => setPhase(1) }, // Fade out text
      { time: 3000, action: () => setPhase(3) }, // Start panning to rocket
      { time: 5000, action: () => setPhase(5) }, // Begin countdown
      {
        time: 5000,
        action: () => {
          setCountdown(3)
          // audioRefs.current.countdown?.play()
        }
      },
      {
        time: 6000,
        action: () => {
          setCountdown(2)
          setShakeIntensity(1)
          // audioRefs.current.countdown?.play()
        }
      },
      {
        time: 7000,
        action: () => {
          setCountdown(1)
          setShakeIntensity(2)
          // audioRefs.current.countdown?.play()
        }
      },
      {
        time: 8000,
        action: () => {
          setCountdown(0)
          setPhase(8)
          setShakeIntensity(3)
          // audioRefs.current.ignition?.play()
        }
      },
      {
        time: 8500,
        action: () => {
          setParticleIntensity(1)
          setShakeIntensity(5)
        }
      },
      {
        time: 10000,
        action: () => {
          setIsLaunching(true)
          setPhase(10)
          setShakeIntensity(8)
          // audioRefs.current.launch?.play()
          // audioRefs.current.fly?.play()
        }
      },
      {
        time: 10100,
        action: () => {
          // Start moving rocket upward
          const moveRocket = setInterval(() => {
            setRocketPosition(prev => [prev[0], prev[1] + 0.3, prev[2]])
          }, 50)

          setTimeout(() => clearInterval(moveRocket), 3000)
        }
      },
      { time: 13000, action: () => setPhase(13) }, // Rocket off screen
      { time: 14000, action: () => setPhase(14) }, // Begin fade out
      { time: 15000, action: () => onComplete() } // Complete!
    ]

    const timeouts = timeline.map(({ time, action }) => setTimeout(action, time))

    return () => timeouts.forEach(clearTimeout)
  }, [onComplete])

  // Calculate shake transform based on intensity
  const getShakeTransform = () => {
    if (shakeIntensity === 0) return 'translate(0, 0)'
    const maxOffset = shakeIntensity * 2
    const x = (Math.random() - 0.5) * maxOffset
    const y = (Math.random() - 0.5) * maxOffset
    return `translate(${x}px, ${y}px)`
  }

  return (
    <div className="fixed inset-0 z-50 bg-black">
      <style>{`
        @keyframes shake {
          0%, 100% { transform: translate(0, 0); }
          25% { transform: translate(var(--shake-x), var(--shake-y)); }
          50% { transform: translate(calc(var(--shake-x) * -1), var(--shake-y)); }
          75% { transform: translate(var(--shake-x), calc(var(--shake-y) * -1)); }
        }
      `}</style>

      {/* Screen shake container */}
      <div
        style={{
          transform: getShakeTransform(),
          transition: shakeIntensity > 0 ? 'none' : 'transform 0.5s ease-out',
          width: '100%',
          height: '100%'
        }}
      >
        {/* Three.js Canvas */}
        <Canvas>
          <PerspectiveCamera makeDefault position={[0, 10, 15]} fov={60} />
          <CameraController phase={phase} />

          {/* Lighting */}
          <ambientLight intensity={0.4} />
          <pointLight position={[10, 10, 10]} intensity={1} />
          <pointLight position={[-10, 5, -10]} intensity={0.5} color="#3498db" />

          {/* Rocket */}
          <RocketModel position={rocketPosition} isLaunching={isLaunching} />

          {/* Particle Effects */}
          <ParticleSystem
            isActive={particleIntensity > 0}
            intensity={particleIntensity}
            type="fire"
            position={rocketPosition}
          />
          <ParticleSystem
            isActive={particleIntensity > 0}
            intensity={particleIntensity * 0.7}
            type="smoke"
            position={rocketPosition}
          />

          {/* Ground plane */}
          <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -1.8, 0]}>
            <planeGeometry args={[50, 50]} />
            <meshStandardMaterial color="#2c3e50" roughness={0.8} />
          </mesh>

          {/* Stars in background */}
          {Array.from({ length: 100 }).map((_, i) => (
            <mesh
              key={i}
              position={[
                (Math.random() - 0.5) * 100,
                Math.random() * 50 + 10,
                (Math.random() - 0.5) * 100
              ]}
            >
              <sphereGeometry args={[0.1, 4, 4]} />
              <meshBasicMaterial color="white" />
            </mesh>
          ))}
        </Canvas>

        {/* Text Overlays */}
        <div className="absolute inset-0 pointer-events-none flex items-center justify-center">
          <AnimatePresence>
            {/* Phase 0-1: "Launch in T-10 seconds" */}
            {phase >= 0 && phase < 2 && (
              <motion.div
                initial={{ opacity: 0, scale: 0.8 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 1.2 }}
                transition={{ duration: 0.8 }}
                className="text-white text-6xl md:text-8xl font-bold text-center"
                style={{ textShadow: '0 0 30px rgba(255,255,255,0.5)' }}
              >
                Launch in T-10 seconds
              </motion.div>
            )}

            {/* Phase 5-8: Countdown */}
            {phase >= 5 && countdown > 0 && (
              <motion.div
                key={countdown}
                initial={{ opacity: 0, scale: 0.5 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 1.5 }}
                transition={{ duration: 0.4 }}
                className="text-red-500 font-bold text-center"
                style={{
                  fontSize: `${10 + (4 - countdown) * 2}rem`,
                  textShadow: '0 0 50px rgba(239, 68, 68, 0.8)'
                }}
              >
                T-{countdown}
              </motion.div>
            )}

            {/* Phase 8: T-0 / Ignition */}
            {phase >= 8 && phase < 10 && (
              <motion.div
                initial={{ opacity: 0, scale: 0.5 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ duration: 0.3 }}
                className="text-orange-500 font-bold text-center"
                style={{
                  fontSize: '16rem',
                  textShadow: '0 0 80px rgba(249, 115, 22, 1)'
                }}
              >
                IGNITION
              </motion.div>
            )}

            {/* Phase 14: Fade out */}
            {phase >= 14 && (
              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ duration: 1 }}
                className="absolute inset-0 bg-black"
              />
            )}
          </AnimatePresence>
        </div>
      </div>
    </div>
  )
}
