import { useState, useEffect, useRef } from 'react'
import { Canvas, useFrame, useThree } from '@react-three/fiber'
import { PerspectiveCamera, Stars, Environment } from '@react-three/drei'
import { EffectComposer, Bloom, DepthOfField, Vignette, ChromaticAberration } from '@react-three/postprocessing'
import { BlendFunction } from 'postprocessing'
import { motion, AnimatePresence } from 'framer-motion'
import { RocketModel } from './rocket-model'
import { ParticleSystem } from './particle-system'
import * as THREE from 'three'

interface RocketLaunchSequenceProps {
  onComplete: () => void
  projectId?: string | null
}

// Cinematic Camera Controller with smooth transitions
function CameraController({ phase, rocketY }: { phase: number; rocketY: number }) {
  const { camera } = useThree()
  const targetPosition = useRef(new THREE.Vector3())
  const targetLookAt = useRef(new THREE.Vector3())

  useFrame((state, delta) => {
    // Define camera positions for each phase
    let camPos: THREE.Vector3
    let lookPos: THREE.Vector3

    if (phase < 3) {
      // Phase 0-3: Wide establishing shot
      camPos = new THREE.Vector3(8, 6, 12)
      lookPos = new THREE.Vector3(0, 2, 0)
    } else if (phase < 5) {
      // Phase 3-5: Slow push in
      const t = (phase - 3) / 2
      camPos = new THREE.Vector3(
        THREE.MathUtils.lerp(8, 5, t),
        THREE.MathUtils.lerp(6, 4, t),
        THREE.MathUtils.lerp(12, 9, t)
      )
      lookPos = new THREE.Vector3(0, 2, 0)
    } else if (phase < 8) {
      // Phase 5-8: Countdown - dramatic low angle
      const t = (phase - 5) / 3
      camPos = new THREE.Vector3(
        THREE.MathUtils.lerp(5, 4, t),
        THREE.MathUtils.lerp(4, 1.5, t),
        THREE.MathUtils.lerp(9, 8, t)
      )
      lookPos = new THREE.Vector3(0, 2 + t * 1, 0)
    } else if (phase < 10) {
      // Phase 8-10: Ignition - hold dramatic angle
      camPos = new THREE.Vector3(4, 1.5, 8)
      lookPos = new THREE.Vector3(0, 0, 0)
    } else if (phase < 11) {
      // Phase 10-11: Liftoff - quick zoom out
      const t = (phase - 10)
      camPos = new THREE.Vector3(
        THREE.MathUtils.lerp(4, 6, t),
        THREE.MathUtils.lerp(1.5, 3, t),
        THREE.MathUtils.lerp(8, 10, t)
      )
      lookPos = new THREE.Vector3(0, rocketY * 0.5, 0)
    } else {
      // Phase 11+: Follow rocket upward
      const t = Math.min((phase - 11) / 2, 1)
      camPos = new THREE.Vector3(
        6 + Math.sin(state.clock.getElapsedTime() * 0.3) * 0.5,
        3 + rocketY * 0.4,
        10 - t * 2
      )
      lookPos = new THREE.Vector3(0, rocketY, 0)
    }

    // Smooth interpolation
    targetPosition.current.lerp(camPos, delta * 2)
    targetLookAt.current.lerp(lookPos, delta * 2)

    camera.position.copy(targetPosition.current)
    camera.lookAt(targetLookAt.current)

    // Add subtle camera shake during launch
    if (phase >= 8 && phase < 12) {
      const intensity = phase >= 10 ? 0.15 : 0.08
      camera.position.x += (Math.random() - 0.5) * intensity
      camera.position.y += (Math.random() - 0.5) * intensity
      camera.rotation.z += (Math.random() - 0.5) * 0.01
    }
  })

  return null
}

// Dynamic lighting that responds to rocket state
function DynamicLighting({ phase, ignitionActive, isLaunching }: {
  phase: number
  ignitionActive: boolean
  isLaunching: boolean
}) {
  const engineLightRef = useRef<THREE.PointLight>(null!)
  const ambientLightRef = useRef<THREE.AmbientLight>(null!)

  useFrame(() => {
    if (engineLightRef.current) {
      const baseIntensity = ignitionActive ? 8 : 0
      const launchIntensity = isLaunching ? 15 : baseIntensity
      engineLightRef.current.intensity = launchIntensity + Math.random() * 3
    }

    // Dim ambient light during launch for dramatic effect
    if (ambientLightRef.current) {
      const targetIntensity = isLaunching ? 0.2 : 0.5
      ambientLightRef.current.intensity += (targetIntensity - ambientLightRef.current.intensity) * 0.1
    }
  })

  return (
    <>
      <ambientLight ref={ambientLightRef} intensity={0.5} color="#b8d4f1" />
      <directionalLight
        position={[10, 10, 5]}
        intensity={1.2}
        castShadow
        shadow-mapSize-width={2048}
        shadow-mapSize-height={2048}
        shadow-camera-far={50}
        shadow-camera-left={-10}
        shadow-camera-right={10}
        shadow-camera-top={10}
        shadow-camera-bottom={-10}
      />
      <pointLight
        ref={engineLightRef}
        position={[0, -0.5, 0]}
        intensity={0}
        color="#ff6b35"
        distance={20}
        decay={2}
      />
      <hemisphereLight
        intensity={0.3}
        color="#87ceeb"
        groundColor="#2c3e50"
      />
    </>
  )
}

// Ground with modern grid pattern
function LaunchGround() {
  return (
    <group>
      {/* Main ground plane */}
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -1.8, 0]} receiveShadow>
        <planeGeometry args={[100, 100]} />
        <meshStandardMaterial
          color="#1a252f"
          metalness={0.3}
          roughness={0.7}
        />
      </mesh>

      {/* Grid lines */}
      <gridHelper args={[100, 50, '#e63946', '#34495e']} position={[0, -1.79, 0]} />

      {/* Accent circles around launch pad */}
      {[3, 5, 7].map((radius, i) => (
        <mesh key={i} rotation={[-Math.PI / 2, 0, 0]} position={[0, -1.78, 0]}>
          <ringGeometry args={[radius, radius + 0.05, 64]} />
          <meshBasicMaterial
            color="#e63946"
            transparent
            opacity={0.3 - i * 0.08}
          />
        </mesh>
      ))}
    </group>
  )
}

export function RocketLaunchSequence({ onComplete, projectId }: RocketLaunchSequenceProps) {
  const [phase, setPhase] = useState(0)
  const [countdown, setCountdown] = useState(3)
  const [rocketY, setRocketY] = useState(0)
  const [isLaunching, setIsLaunching] = useState(false)
  const [ignitionActive, setIgnitionActive] = useState(false)
  const [particleIntensity, setParticleIntensity] = useState(0)

  // Animation timeline - more dramatic pacing
  useEffect(() => {
    const timeline = [
      { time: 0, action: () => setPhase(0) },
      { time: 1500, action: () => setPhase(1) },
      { time: 2500, action: () => setPhase(3) }, // Start camera movement
      { time: 4000, action: () => setPhase(5) }, // Begin countdown
      {
        time: 4000,
        action: () => {
          setCountdown(3)
        }
      },
      {
        time: 5000,
        action: () => {
          setCountdown(2)
        }
      },
      {
        time: 6000,
        action: () => {
          setCountdown(1)
        }
      },
      {
        time: 7000,
        action: () => {
          setCountdown(0)
          setPhase(8)
          setIgnitionActive(true)
        }
      },
      {
        time: 7500,
        action: () => {
          setParticleIntensity(1)
        }
      },
      {
        time: 9000,
        action: () => {
          setIsLaunching(true)
          setPhase(10)
        }
      },
      {
        time: 9100,
        action: () => {
          // Animate rocket upward
          let currentY = 0
          const moveInterval = setInterval(() => {
            currentY += 0.4
            setRocketY(currentY)
          }, 50)

          setTimeout(() => clearInterval(moveInterval), 3500)
        }
      },
      { time: 12500, action: () => setPhase(13) },
      { time: 13500, action: () => setPhase(14) },
      { time: 14500, action: () => onComplete() }
    ]

    const timeouts = timeline.map(({ time, action }) => setTimeout(action, time))
    return () => timeouts.forEach(clearTimeout)
  }, [onComplete])

  return (
    <div className="fixed inset-0 z-50 bg-black overflow-hidden">
      {/* Three.js Scene */}
      <Canvas shadows>
        <PerspectiveCamera makeDefault position={[8, 6, 12]} fov={50} />
        <CameraController phase={phase} rocketY={rocketY} />

        {/* Lighting */}
        <DynamicLighting phase={phase} ignitionActive={ignitionActive} isLaunching={isLaunching} />

        {/* Environment */}
        <Stars radius={100} depth={50} count={5000} factor={4} saturation={0} fade speed={1} />
        <fog attach="fog" args={['#0a0e1a', 20, 60]} />

        {/* Ground */}
        <LaunchGround />

        {/* Rocket */}
        <RocketModel
          position={[0, rocketY, 0]}
          isLaunching={isLaunching}
          ignitionActive={ignitionActive}
        />

        {/* Multi-layer Particle Systems */}
        <ParticleSystem
          isActive={particleIntensity > 0}
          intensity={particleIntensity}
          type="fire"
          position={[0, rocketY, 0]}
        />
        <ParticleSystem
          isActive={particleIntensity > 0}
          intensity={particleIntensity * 0.8}
          type="smoke"
          position={[0, rocketY, 0]}
        />
        <ParticleSystem
          isActive={particleIntensity > 0}
          intensity={particleIntensity}
          type="exhaust"
          position={[0, rocketY, 0]}
        />
        <ParticleSystem
          isActive={ignitionActive}
          intensity={particleIntensity * 1.2}
          type="sparks"
          position={[0, rocketY, 0]}
        />

        {/* Post-processing Effects */}
        <EffectComposer>
          <Bloom
            intensity={isLaunching ? 2.5 : ignitionActive ? 1.5 : 0.5}
            luminanceThreshold={0.2}
            luminanceSmoothing={0.9}
            height={300}
            mipmapBlur
          />
          <DepthOfField
            focusDistance={0.01}
            focalLength={0.05}
            bokehScale={phase < 8 ? 2 : 4}
          />
          <Vignette
            offset={0.3}
            darkness={0.5}
            eskil={false}
            blendFunction={BlendFunction.NORMAL}
          />
          {isLaunching && (
            <ChromaticAberration
              offset={[0.002, 0.002]}
              blendFunction={BlendFunction.NORMAL}
            />
          )}
        </EffectComposer>
      </Canvas>

      {/* UI Overlays */}
      <div className="absolute inset-0 pointer-events-none">
        <AnimatePresence>
          {/* Phase 0-1: Mission title */}
          {phase >= 0 && phase < 2 && (
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
              transition={{ duration: 0.8, ease: 'easeOut' }}
              className="absolute inset-0 flex items-center justify-center"
            >
              <div className="text-center">
                <motion.div
                  initial={{ scale: 0.9, opacity: 0 }}
                  animate={{ scale: 1, opacity: 1 }}
                  transition={{ delay: 0.2, duration: 0.6 }}
                  className="text-white font-bold mb-4"
                  style={{
                    fontSize: '4rem',
                    textShadow: '0 0 30px rgba(230, 57, 70, 0.8), 0 0 60px rgba(230, 57, 70, 0.4)',
                    fontFamily: 'monospace',
                    letterSpacing: '0.1em'
                  }}
                >
                  PROJECT LAUNCH
                </motion.div>
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  transition={{ delay: 0.6, duration: 0.6 }}
                  className="text-gray-400 text-xl tracking-widest"
                  style={{ fontFamily: 'monospace' }}
                >
                  MISSION CONTROL: INITIATING SEQUENCE
                </motion.div>
              </div>
            </motion.div>
          )}

          {/* Phase 5-8: Countdown with glitch effect */}
          {phase >= 5 && countdown > 0 && (
            <motion.div
              key={countdown}
              initial={{ opacity: 0, scale: 0.3, rotate: -10 }}
              animate={{ opacity: 1, scale: 1, rotate: 0 }}
              exit={{ opacity: 0, scale: 1.5, rotate: 10 }}
              transition={{ duration: 0.3, ease: 'easeOut' }}
              className="absolute inset-0 flex flex-col items-center justify-center"
            >
              {/* Glitch background */}
              <motion.div
                animate={{
                  opacity: [0.1, 0.3, 0.1],
                  scale: [1, 1.1, 1]
                }}
                transition={{ duration: 0.15, repeat: Infinity }}
                className="absolute bg-red-500 blur-3xl"
                style={{
                  width: '300px',
                  height: '300px',
                  borderRadius: '50%',
                  opacity: 0.2
                }}
              />

              <div className="relative">
                {/* Main countdown number */}
                <div
                  className="text-red-500 font-bold text-center relative"
                  style={{
                    fontSize: `${12 + (4 - countdown) * 4}rem`,
                    textShadow: '0 0 40px rgba(239, 68, 68, 1), 0 0 80px rgba(239, 68, 68, 0.6)',
                    fontFamily: 'monospace',
                    fontWeight: 900
                  }}
                >
                  T-{countdown}
                  {/* Scanline effect */}
                  <motion.div
                    animate={{ y: [0, 100] }}
                    transition={{ duration: 0.5, repeat: Infinity, ease: 'linear' }}
                    className="absolute inset-0 bg-gradient-to-b from-transparent via-red-500 to-transparent opacity-20"
                    style={{ height: '20px' }}
                  />
                </div>

                {/* Status text */}
                <motion.div
                  animate={{ opacity: [1, 0.5, 1] }}
                  transition={{ duration: 0.5, repeat: Infinity }}
                  className="text-center mt-4 text-red-400 tracking-widest"
                  style={{ fontFamily: 'monospace', fontSize: '1.2rem' }}
                >
                  [ ENGINES ARMED ]
                </motion.div>
              </div>
            </motion.div>
          )}

          {/* Phase 8-10: IGNITION */}
          {phase >= 8 && phase < 10 && (
            <motion.div
              initial={{ opacity: 0, scale: 0.5 }}
              animate={{ opacity: [0, 1, 1, 0], scale: [0.5, 1.2, 1.2, 1.5] }}
              transition={{ duration: 1.5 }}
              className="absolute inset-0 flex items-center justify-center"
            >
              <div className="relative">
                <div
                  className="text-orange-500 font-bold text-center"
                  style={{
                    fontSize: '14rem',
                    textShadow: '0 0 60px rgba(249, 115, 22, 1), 0 0 120px rgba(249, 115, 22, 0.8)',
                    fontFamily: 'monospace',
                    fontWeight: 900
                  }}
                >
                  IGNITION
                </div>
                {/* Pulsing glow */}
                <motion.div
                  animate={{
                    opacity: [0.3, 0.6, 0.3],
                    scale: [1, 1.2, 1]
                  }}
                  transition={{ duration: 0.5, repeat: Infinity }}
                  className="absolute inset-0 bg-orange-500 blur-3xl"
                  style={{ opacity: 0.3 }}
                />
              </div>
            </motion.div>
          )}

          {/* Phase 10+: LIFTOFF */}
          {phase >= 10 && phase < 13 && (
            <motion.div
              initial={{ opacity: 0, y: 50 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -50 }}
              transition={{ duration: 0.6 }}
              className="absolute inset-0 flex items-center justify-center"
            >
              <div className="text-center">
                <motion.div
                  animate={{
                    scale: [1, 1.05, 1],
                    textShadow: [
                      '0 0 40px rgba(74, 222, 128, 1)',
                      '0 0 80px rgba(74, 222, 128, 0.8)',
                      '0 0 40px rgba(74, 222, 128, 1)'
                    ]
                  }}
                  transition={{ duration: 1, repeat: Infinity }}
                  className="text-green-400 font-bold"
                  style={{
                    fontSize: '10rem',
                    fontFamily: 'monospace',
                    fontWeight: 900
                  }}
                >
                  LIFTOFF
                </motion.div>
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  transition={{ delay: 0.3 }}
                  className="text-gray-400 tracking-widest mt-4"
                  style={{ fontFamily: 'monospace', fontSize: '1.5rem' }}
                >
                  [ ALL SYSTEMS NOMINAL ]
                </motion.div>
              </div>
            </motion.div>
          )}

          {/* HUD Elements */}
          {phase >= 5 && phase < 14 && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="absolute inset-0"
            >
              {/* Top left - Mission info */}
              <div
                className="absolute top-8 left-8 text-cyan-400 space-y-1"
                style={{ fontFamily: 'monospace', fontSize: '0.9rem' }}
              >
                <div className="opacity-70">MISSION: PROJECT DEPLOYMENT</div>
                <div className="opacity-70">ALTITUDE: {Math.round(rocketY * 10)} M</div>
                <div className="opacity-70">STATUS: {isLaunching ? 'ASCENDING' : ignitionActive ? 'IGNITION' : 'STANDBY'}</div>
              </div>

              {/* Bottom right - System status */}
              <div
                className="absolute bottom-8 right-8 text-green-400 space-y-1 text-right"
                style={{ fontFamily: 'monospace', fontSize: '0.9rem' }}
              >
                <motion.div
                  animate={{ opacity: [0.5, 1, 0.5] }}
                  transition={{ duration: 1, repeat: Infinity }}
                >
                  ENGINES: {ignitionActive || isLaunching ? 'ACTIVE' : 'STANDBY'}
                </motion.div>
                <div className="opacity-70">FUEL: {Math.max(0, 100 - Math.round(rocketY * 2))}%</div>
                <div className="opacity-70">TRAJECTORY: NOMINAL</div>
              </div>

              {/* Targeting reticle */}
              <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                <svg width="200" height="200" className="opacity-30">
                  <circle
                    cx="100"
                    cy="100"
                    r="80"
                    fill="none"
                    stroke="#22d3ee"
                    strokeWidth="1"
                    strokeDasharray="5,5"
                  />
                  <line x1="20" y1="100" x2="40" y2="100" stroke="#22d3ee" strokeWidth="1" />
                  <line x1="160" y1="100" x2="180" y2="100" stroke="#22d3ee" strokeWidth="1" />
                  <line x1="100" y1="20" x2="100" y2="40" stroke="#22d3ee" strokeWidth="1" />
                  <line x1="100" y1="160" x2="100" y2="180" stroke="#22d3ee" strokeWidth="1" />
                </svg>
              </div>
            </motion.div>
          )}

          {/* Phase 14: Fade to black */}
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

      {/* Vignette overlay */}
      <div
        className="absolute inset-0 pointer-events-none"
        style={{
          background: 'radial-gradient(circle, transparent 40%, rgba(0,0,0,0.8) 100%)'
        }}
      />
    </div>
  )
}
