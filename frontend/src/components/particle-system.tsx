import { useRef, useMemo } from 'react'
import { useFrame } from '@react-three/fiber'
import * as THREE from 'three'

interface ParticleSystemProps {
  isActive?: boolean
  intensity?: number // 0-1, how intense the effect is
  type?: 'smoke' | 'fire' | 'exhaust' | 'sparks'
  position?: [number, number, number]
}

export function ParticleSystem({
  isActive = false,
  intensity = 0,
  type = 'smoke',
  position = [0, 0, 0]
}: ParticleSystemProps) {
  const particlesRef = useRef<THREE.Points>(null!)
  const velocitiesRef = useRef<Float32Array>(null!)
  const lifetimesRef = useRef<Float32Array>(null!)
  const sizesRef = useRef<Float32Array>(null!)

  // Different particle counts based on type
  const particleCount = useMemo(() => {
    switch (type) {
      case 'fire': return 500
      case 'smoke': return 300
      case 'exhaust': return 400
      case 'sparks': return 200
      default: return 300
    }
  }, [type])

  // Initialize particle system
  const { positions, velocities, lifetimes, sizes, colors } = useMemo(() => {
    const positions = new Float32Array(particleCount * 3)
    const velocities = new Float32Array(particleCount * 3)
    const lifetimes = new Float32Array(particleCount)
    const sizes = new Float32Array(particleCount)
    const colors = new Float32Array(particleCount * 3)

    for (let i = 0; i < particleCount; i++) {
      const i3 = i * 3

      // Position initialization based on type
      if (type === 'sparks') {
        // Sparks come from engine nozzles
        const angle = Math.random() * Math.PI * 2
        const radius = 0.6 * Math.random()
        positions[i3] = Math.cos(angle) * radius
        positions[i3 + 1] = -0.6
        positions[i3 + 2] = Math.sin(angle) * radius
      } else {
        // Fire and smoke from engine cluster (9 engines)
        const engineIndex = Math.floor(Math.random() * 9)
        const enginePositions = [
          [0, 0], // Center
          [0.25, 0], [0, 0.25], [-0.25, 0], [0, -0.25], // Inner ring
          [0.35, 0.35], [-0.35, 0.35], [-0.35, -0.35], [0.35, -0.35] // Outer ring
        ]
        const [ex, ez] = enginePositions[engineIndex] || [0, 0]

        positions[i3] = ex + (Math.random() - 0.5) * 0.15
        positions[i3 + 1] = -0.6 + Math.random() * 0.2
        positions[i3 + 2] = ez + (Math.random() - 0.5) * 0.15
      }

      // Velocity based on type
      if (type === 'fire' || type === 'exhaust') {
        velocities[i3] = (Math.random() - 0.5) * 0.04
        velocities[i3 + 1] = 0.15 + Math.random() * 0.1 // Fast upward
        velocities[i3 + 2] = (Math.random() - 0.5) * 0.04
      } else if (type === 'smoke') {
        velocities[i3] = (Math.random() - 0.5) * 0.08 // More horizontal spread
        velocities[i3 + 1] = 0.08 + Math.random() * 0.06 // Slower upward
        velocities[i3 + 2] = (Math.random() - 0.5) * 0.08
      } else if (type === 'sparks') {
        const angle = Math.random() * Math.PI * 2
        const speed = 0.1 + Math.random() * 0.2
        velocities[i3] = Math.cos(angle) * speed
        velocities[i3 + 1] = 0.05 + Math.random() * 0.15 // Upward with gravity
        velocities[i3 + 2] = Math.sin(angle) * speed
      }

      // Random lifetime start
      lifetimes[i] = Math.random()

      // Initial size
      sizes[i] = type === 'sparks' ? 0.05 + Math.random() * 0.05 : 0.1 + Math.random() * 0.2

      // Color based on type
      if (type === 'fire' || type === 'exhaust') {
        // Orange to yellow gradient
        const t = Math.random()
        colors[i3] = 1.0 // R
        colors[i3 + 1] = 0.4 + t * 0.4 // G
        colors[i3 + 2] = 0.1 + t * 0.2 // B
      } else if (type === 'smoke') {
        // Gray to white
        const gray = 0.5 + Math.random() * 0.3
        colors[i3] = gray
        colors[i3 + 1] = gray
        colors[i3 + 2] = gray
      } else if (type === 'sparks') {
        // Bright white to yellow
        colors[i3] = 1.0
        colors[i3 + 1] = 0.8 + Math.random() * 0.2
        colors[i3 + 2] = 0.3 + Math.random() * 0.3
      }
    }

    velocitiesRef.current = velocities
    lifetimesRef.current = lifetimes
    sizesRef.current = sizes

    return { positions, velocities, lifetimes, sizes, colors }
  }, [type, particleCount])

  const positionsAttribute = useRef(new THREE.BufferAttribute(positions, 3))
  const sizesAttribute = useRef(new THREE.BufferAttribute(sizes, 1))
  const colorsAttribute = useRef(new THREE.BufferAttribute(colors, 3))

  // Animate particles with more sophisticated behavior
  useFrame((state, delta) => {
    if (!isActive || !particlesRef.current || !velocitiesRef.current || !lifetimesRef.current || !sizesRef.current) return

    const positions = positionsAttribute.current.array as Float32Array
    const sizes = sizesAttribute.current.array as Float32Array
    const colors = colorsAttribute.current.array as Float32Array
    const velocities = velocitiesRef.current
    const lifetimes = lifetimesRef.current

    const time = state.clock.getElapsedTime()

    for (let i = 0; i < particleCount; i++) {
      const i3 = i * 3

      // Update lifetime
      lifetimes[i] += delta * intensity * (type === 'sparks' ? 3 : 2)

      if (lifetimes[i] > 1) {
        // Reset particle
        if (type === 'sparks') {
          const angle = Math.random() * Math.PI * 2
          const radius = 0.6 * Math.random()
          positions[i3] = Math.cos(angle) * radius
          positions[i3 + 1] = -0.6
          positions[i3 + 2] = Math.sin(angle) * radius
        } else {
          const engineIndex = Math.floor(Math.random() * 9)
          const enginePositions = [
            [0, 0],
            [0.25, 0], [0, 0.25], [-0.25, 0], [0, -0.25],
            [0.35, 0.35], [-0.35, 0.35], [-0.35, -0.35], [0.35, -0.35]
          ]
          const [ex, ez] = enginePositions[engineIndex] || [0, 0]

          positions[i3] = ex + (Math.random() - 0.5) * 0.15
          positions[i3 + 1] = -0.6 + Math.random() * 0.2
          positions[i3 + 2] = ez + (Math.random() - 0.5) * 0.15
        }

        // Reset velocity
        if (type === 'fire' || type === 'exhaust') {
          velocities[i3] = (Math.random() - 0.5) * 0.04
          velocities[i3 + 1] = 0.15 + Math.random() * 0.1
          velocities[i3 + 2] = (Math.random() - 0.5) * 0.04
        } else if (type === 'smoke') {
          velocities[i3] = (Math.random() - 0.5) * 0.08
          velocities[i3 + 1] = 0.08 + Math.random() * 0.06
          velocities[i3 + 2] = (Math.random() - 0.5) * 0.08
        } else if (type === 'sparks') {
          const angle = Math.random() * Math.PI * 2
          const speed = 0.1 + Math.random() * 0.2
          velocities[i3] = Math.cos(angle) * speed
          velocities[i3 + 1] = 0.05 + Math.random() * 0.15
          velocities[i3 + 2] = Math.sin(angle) * speed
        }

        lifetimes[i] = 0
        sizesRef.current[i] = type === 'sparks' ? 0.05 + Math.random() * 0.05 : 0.1 + Math.random() * 0.2
      } else {
        // Update position
        const life = lifetimes[i]

        // Apply turbulence
        const turbulence = Math.sin(time * 2 + i * 0.1) * 0.02

        positions[i3] += velocities[i3] * intensity + turbulence
        positions[i3 + 2] += velocities[i3 + 2] * intensity + Math.cos(time * 2 + i * 0.1) * 0.02

        // Apply gravity to sparks
        if (type === 'sparks') {
          velocities[i3 + 1] -= 0.008 // Gravity
          positions[i3 + 1] += velocities[i3 + 1] * intensity
        } else {
          positions[i3 + 1] += velocities[i3 + 1] * intensity
        }

        // Expand smoke over time
        if (type === 'smoke') {
          sizesRef.current[i] = (0.1 + Math.random() * 0.2) * (1 + life * 2)
          // Smoke rises slower over time
          velocities[i3 + 1] *= 0.995
        } else if (type === 'fire' || type === 'exhaust') {
          sizesRef.current[i] = (0.1 + Math.random() * 0.2) * (1 + life * 0.5)
        } else if (type === 'sparks') {
          sizesRef.current[i] = (0.05 + Math.random() * 0.05) * Math.max(0, 1 - life)
        }

        // Update color/opacity based on lifetime
        if (type === 'fire' || type === 'exhaust') {
          // Fire particles fade from bright to dark
          const brightness = Math.max(0, 1 - life * 0.8)
          colors[i3] = 1.0 * brightness
          colors[i3 + 1] = (0.4 + Math.random() * 0.4) * brightness
          colors[i3 + 2] = (0.1 + Math.random() * 0.2) * brightness
        } else if (type === 'smoke') {
          // Smoke darkens over time
          const darkness = 0.5 - life * 0.3
          colors[i3] = darkness
          colors[i3 + 1] = darkness
          colors[i3 + 2] = darkness
        } else if (type === 'sparks') {
          // Sparks dim over time
          const sparkBrightness = Math.max(0, 1 - life)
          colors[i3] = 1.0 * sparkBrightness
          colors[i3 + 1] = (0.8 + Math.random() * 0.2) * sparkBrightness
          colors[i3 + 2] = (0.3 + Math.random() * 0.3) * sparkBrightness
        }
      }

      sizes[i] = sizesRef.current[i]
    }

    positionsAttribute.current.needsUpdate = true
    sizesAttribute.current.needsUpdate = true
    colorsAttribute.current.needsUpdate = true
  })

  // Determine blending and opacity based on type
  const blending = type === 'smoke' ? THREE.NormalBlending : THREE.AdditiveBlending
  const baseOpacity = type === 'smoke' ? 0.4 : type === 'sparks' ? 1.0 : 0.8

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
          attach="attributes-size"
          count={particleCount}
          array={sizesAttribute.current.array}
          itemSize={1}
        />
        <bufferAttribute
          attach="attributes-color"
          count={particleCount}
          array={colorsAttribute.current.array}
          itemSize={3}
        />
      </bufferGeometry>
      <pointsMaterial
        size={type === 'sparks' ? 0.08 : 0.3}
        sizeAttenuation={true}
        transparent
        opacity={baseOpacity}
        depthWrite={false}
        blending={blending}
        vertexColors={true}
      />
    </points>
  )
}
