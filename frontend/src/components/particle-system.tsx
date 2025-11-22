import { useRef } from 'react'
import { useFrame } from '@react-three/fiber'
import * as THREE from 'three'

interface ParticleSystemProps {
  isActive?: boolean
  intensity?: number
  type?: 'confetti'
  position?: [number, number, number]
}

export function ParticleSystem({
  isActive = false,
  intensity = 0,
  type = 'confetti',
  position = [0, 0, 0]
}: ParticleSystemProps) {
  const particlesRef = useRef<THREE.Points>(null!)
  const particleCount = 150
  const particleData = useRef<{
    positions: Float32Array
    velocities: Float32Array
    rotations: Float32Array
    rotationSpeeds: Float32Array
    lifetimes: Float32Array
    colors: Float32Array
  }>({
    positions: new Float32Array(particleCount * 3),
    velocities: new Float32Array(particleCount * 3),
    rotations: new Float32Array(particleCount),
    rotationSpeeds: new Float32Array(particleCount),
    lifetimes: new Float32Array(particleCount),
    colors: new Float32Array(particleCount * 3)
  })

  // Confetti colors - vibrant and fun!
  const confettiColors = [
    [1.0, 0.2, 0.4],   // Pink
    [0.2, 0.8, 1.0],   // Cyan
    [1.0, 0.8, 0.2],   // Gold
    [0.5, 0.2, 1.0],   // Purple
    [1.0, 0.4, 0.2],   // Orange
    [0.2, 1.0, 0.4],   // Green
  ]

  // Animate confetti
  useFrame((state, delta) => {
    if (!isActive || !particlesRef.current) return

    const { positions, velocities, rotations, rotationSpeeds, lifetimes, colors } = particleData.current

    for (let i = 0; i < particleCount; i++) {
      const i3 = i * 3

      // Update lifetime
      lifetimes[i] = (lifetimes[i] || 0) + delta * intensity * 1.5

      if (lifetimes[i]! > 1) {
        // Reset particle - spawn from rocket
        const spreadX = (Math.random() - 0.5) * 0.5
        const spreadZ = (Math.random() - 0.5) * 0.5

        positions[i3] = spreadX
        positions[i3 + 1] = -2 + Math.random() * 0.5
        positions[i3 + 2] = spreadZ

        // Confetti shoots out in all directions with upward bias
        const angle = Math.random() * Math.PI * 2
        const speed = 0.15 + Math.random() * 0.15
        velocities[i3] = Math.cos(angle) * speed
        velocities[i3 + 1] = 0.2 + Math.random() * 0.3 // Upward
        velocities[i3 + 2] = Math.sin(angle) * speed

        // Random rotation
        rotations[i] = Math.random() * Math.PI * 2
        rotationSpeeds[i] = (Math.random() - 0.5) * 0.2

        // Random vibrant color
        const colorIndex = Math.floor(Math.random() * confettiColors.length)
        const color = confettiColors[colorIndex] || [1, 1, 1]
        colors[i3] = color[0]!
        colors[i3 + 1] = color[1]!
        colors[i3 + 2] = color[2]!

        lifetimes[i] = 0
      } else {
        // Move particle
        const life = lifetimes[i]!

        // Apply gravity over time
        velocities[i3 + 1] = (velocities[i3 + 1] || 0) - delta * 0.5

        positions[i3] = (positions[i3] || 0) + (velocities[i3] || 0) * intensity
        positions[i3 + 1] = (positions[i3 + 1] || 0) + (velocities[i3 + 1] || 0) * intensity
        positions[i3 + 2] = (positions[i3 + 2] || 0) + (velocities[i3 + 2] || 0) * intensity

        // Rotate confetti
        rotations[i] = (rotations[i] || 0) + (rotationSpeeds[i] || 0)

        // Fade out near end of life
        const opacity = life > 0.7 ? (1 - life) / 0.3 : 1
        colors[i3] = (colors[i3] || 1) * opacity
        colors[i3 + 1] = (colors[i3 + 1] || 1) * opacity
        colors[i3 + 2] = (colors[i3 + 2] || 1) * opacity
      }
    }

    const posAttr = particlesRef.current.geometry.attributes.position
    const colorAttr = particlesRef.current.geometry.attributes.color

    if (posAttr) posAttr.needsUpdate = true
    if (colorAttr) colorAttr.needsUpdate = true
  })

  return (
    <points ref={particlesRef} position={position}>
      <bufferGeometry>
        <bufferAttribute
          attach="attributes-position"
          count={particleCount}
          array={particleData.current.positions}
          itemSize={3}
        />
        <bufferAttribute
          attach="attributes-color"
          count={particleCount}
          array={particleData.current.colors}
          itemSize={3}
        />
      </bufferGeometry>
      <pointsMaterial
        size={0.15}
        vertexColors
        transparent
        opacity={0.9}
        depthWrite={false}
        blending={THREE.AdditiveBlending}
      />
    </points>
  )
}
