import { useRef } from 'react'
import { useFrame } from '@react-three/fiber'
import * as THREE from 'three'

interface RocketModelProps {
  position?: [number, number, number]
  isLaunching?: boolean
  vibrationIntensity?: number
  engineGlow?: number
}

export function RocketModel({
  position = [0, 0, 0],
  isLaunching = false,
  vibrationIntensity = 0,
  engineGlow = 0
}: RocketModelProps) {
  const rocketRef = useRef<THREE.Group>(null!)

  // Subtle idle sway - very smooth
  useFrame((state) => {
    if (!rocketRef.current) return
    const time = state.clock.getElapsedTime()

    // Very gentle sway, no vibration
    rocketRef.current.rotation.z = Math.sin(time * 0.3) * 0.003
  })

  return (
    <group ref={rocketRef} position={position}>
      {/* Simple 2D low-poly rocket - flat shapes */}

      {/* Body - rectangle */}
      <mesh position={[0, 0, 0]}>
        <boxGeometry args={[0.8, 3, 0.1]} />
        <meshBasicMaterial color="#f8f9fa" />
      </mesh>

      {/* Nose - triangle */}
      <mesh position={[0, 1.8, 0]}>
        <coneGeometry args={[0.4, 0.8, 3]} />
        <meshBasicMaterial color="#e63946" />
      </mesh>

      {/* Window */}
      <mesh position={[0, 1, 0.06]}>
        <circleGeometry args={[0.2, 16]} />
        <meshBasicMaterial color="#3498db" />
      </mesh>

      {/* Fins - 2 triangular fins on sides */}
      {[-1, 1].map((side, i) => (
        <mesh key={`fin-${i}`} position={[side * 0.5, -1, 0]} rotation={[0, 0, side * Math.PI / 4]}>
          <coneGeometry args={[0.5, 0.8, 3]} />
          <meshBasicMaterial color="#22d3ee" />
        </mesh>
      ))}

      {/* Engine - simple rectangle at bottom */}
      <mesh position={[0, -1.8, 0]}>
        <boxGeometry args={[0.6, 0.4, 0.15]} />
        <meshBasicMaterial color="#34495e" />
      </mesh>

      {/* Flame/exhaust when launching */}
      {engineGlow > 0 && (
        <>
          <mesh position={[0, -2.2, 0]}>
            <coneGeometry args={[0.3, 0.8 + engineGlow * 0.05, 3]} />
            <meshBasicMaterial
              color="#ff6b35"
              transparent
              opacity={Math.min(engineGlow / 15, 1)}
            />
          </mesh>

          <pointLight
            color="#ff6b35"
            intensity={engineGlow * 2}
            distance={8}
            position={[0, -2, 0]}
          />
        </>
      )}

      {/* Accent stripe */}
      <mesh position={[0, 0.5, 0.06]}>
        <boxGeometry args={[0.82, 0.2, 0.01]} />
        <meshBasicMaterial color="#22d3ee" />
      </mesh>
    </group>
  )
}
