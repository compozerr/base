import { useRef, useMemo } from 'react'
import { useFrame } from '@react-three/fiber'
import * as THREE from 'three'

interface ParticleSystemProps {
  isActive?: boolean
  intensity?: number // 0-1, how intense the effect is
  type?: 'smoke' | 'fire'
  position?: [number, number, number]
}

export function ParticleSystem({
  isActive = false,
  intensity = 0,
  type = 'smoke',
  position = [0, 0, 0]
}: ParticleSystemProps) {
  const particlesRef = useRef<THREE.Points>(null!)
  const particleCount = 200

  // Initialize particle positions and velocities
  const particles = useMemo(() => {
    const positions = new Float32Array(particleCount * 3)
    const velocities = new Float32Array(particleCount * 3)
    const lifetimes = new Float32Array(particleCount)

    for (let i = 0; i < particleCount; i++) {
      const i3 = i * 3

      // Random position around the engine nozzles (3 of them in a circle)
      const angle = (Math.floor(i / (particleCount / 3)) * 120 + Math.random() * 30) * Math.PI / 180
      const radius = 0.3

      positions[i3] = Math.cos(angle) * radius * (0.5 + Math.random() * 0.5)
      positions[i3 + 1] = -0.7 // Start at engine level
      positions[i3 + 2] = Math.sin(angle) * radius * (0.5 + Math.random() * 0.5)

      // Velocity
      velocities[i3] = (Math.random() - 0.5) * 0.02 // slight horizontal drift
      velocities[i3 + 1] = type === 'fire' ? 0.08 + Math.random() * 0.05 : 0.05 + Math.random() * 0.03 // upward
      velocities[i3 + 2] = (Math.random() - 0.5) * 0.02

      // Lifetime
      lifetimes[i] = Math.random()
    }

    return { positions, velocities, lifetimes }
  }, [type, particleCount])

  const positionsAttribute = useRef(new THREE.BufferAttribute(particles.positions, 3))
  const opacitiesAttribute = useRef(new THREE.BufferAttribute(new Float32Array(particleCount), 1))

  // Animate particles
  useFrame((state, delta) => {
    if (!isActive || !particlesRef.current) return

    const positions = positionsAttribute.current.array as Float32Array
    const opacities = opacitiesAttribute.current.array as Float32Array

    for (let i = 0; i < particleCount; i++) {
      const i3 = i * 3

      // Update lifetime
      const currentLife = particles.lifetimes[i] ?? 0
      particles.lifetimes[i] = currentLife + delta * intensity * 2

      if ((particles.lifetimes[i] ?? 0) > 1) {
        // Reset particle
        const angle = (Math.floor(i / (particleCount / 3)) * 120 + Math.random() * 30) * Math.PI / 180
        const radius = 0.3

        positions[i3] = Math.cos(angle) * radius * (0.5 + Math.random() * 0.5)
        positions[i3 + 1] = -0.7
        positions[i3 + 2] = Math.sin(angle) * radius * (0.5 + Math.random() * 0.5)

        particles.lifetimes[i] = 0
      } else {
        // Move particle
        positions[i3] = (positions[i3] ?? 0) + (particles.velocities[i3] ?? 0) * intensity
        positions[i3 + 1] = (positions[i3 + 1] ?? 0) + (particles.velocities[i3 + 1] ?? 0) * intensity
        positions[i3 + 2] = (positions[i3 + 2] ?? 0) + (particles.velocities[i3 + 2] ?? 0) * intensity
      }

      // Calculate opacity based on lifetime and type
      const life = particles.lifetimes[i] ?? 0
      if (type === 'fire') {
        // Fire particles fade quickly
        opacities[i] = Math.max(0, 1 - life) * intensity * 0.8
      } else {
        // Smoke particles fade slower and expand
        opacities[i] = Math.max(0, 1 - life * 0.7) * intensity * 0.6
      }
    }

    positionsAttribute.current.needsUpdate = true
    opacitiesAttribute.current.needsUpdate = true
  })

  const particleSize = type === 'fire' ? 0.15 : 0.25

  return (
    <points ref={particlesRef} position={position}>
      <bufferGeometry>
        <bufferAttribute
          attach="attributes-position"
          count={particleCount}
          array={positionsAttribute.current.array}
          itemSize={3}
        />
        <bufferAttribute
          attach="attributes-opacity"
          count={particleCount}
          array={opacitiesAttribute.current.array}
          itemSize={1}
        />
      </bufferGeometry>
      <pointsMaterial
        size={particleSize}
        color={type === 'fire' ? '#ff6b35' : '#95a5a6'}
        transparent
        opacity={0.8}
        depthWrite={false}
        blending={THREE.AdditiveBlending}
        vertexColors={false}
      />
    </points>
  )
}
