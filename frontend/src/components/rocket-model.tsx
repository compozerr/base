import { useRef } from 'react'
import { useFrame } from '@react-three/fiber'
import * as THREE from 'three'

interface RocketModelProps {
  position?: [number, number, number]
  isLaunching?: boolean
  launchProgress?: number
}

export function RocketModel({ position = [0, 0, 0], isLaunching = false, launchProgress = 0 }: RocketModelProps) {
  const rocketRef = useRef<THREE.Group>(null!)

  // Animate rocket tilt/vibration before launch
  useFrame((state) => {
    if (rocketRef.current && !isLaunching) {
      // Subtle vibration before launch
      const time = state.clock.getElapsedTime()
      rocketRef.current.rotation.z = Math.sin(time * 10) * 0.02
    }

    if (rocketRef.current && isLaunching) {
      // Vibrate more intensely during launch
      rocketRef.current.rotation.z = (Math.random() - 0.5) * 0.1
    }
  })

  return (
    <group ref={rocketRef} position={position}>
      {/* Nose Cone */}
      <mesh position={[0, 3, 0]}>
        <coneGeometry args={[0.5, 1.5, 6]} />
        <meshStandardMaterial color="#e74c3c" metalness={0.7} roughness={0.3} />
      </mesh>

      {/* Main Body */}
      <mesh position={[0, 1.5, 0]}>
        <cylinderGeometry args={[0.5, 0.5, 3, 8]} />
        <meshStandardMaterial color="#ecf0f1" metalness={0.6} roughness={0.4} />
      </mesh>

      {/* Stripes for detail */}
      <mesh position={[0, 2.3, 0]}>
        <cylinderGeometry args={[0.51, 0.51, 0.3, 8]} />
        <meshStandardMaterial color="#3498db" metalness={0.5} roughness={0.5} />
      </mesh>

      <mesh position={[0, 1.2, 0]}>
        <cylinderGeometry args={[0.51, 0.51, 0.3, 8]} />
        <meshStandardMaterial color="#3498db" metalness={0.5} roughness={0.5} />
      </mesh>

      {/* Engine Section */}
      <mesh position={[0, -0.3, 0]}>
        <cylinderGeometry args={[0.5, 0.6, 0.6, 8]} />
        <meshStandardMaterial color="#34495e" metalness={0.9} roughness={0.2} />
      </mesh>

      {/* Engine Nozzles - 3 of them */}
      {[0, 120, 240].map((angle, i) => {
        const rad = (angle * Math.PI) / 180
        const x = Math.cos(rad) * 0.3
        const z = Math.sin(rad) * 0.3
        return (
          <group key={i}>
            <mesh position={[x, -0.5, z]}>
              <cylinderGeometry args={[0.15, 0.2, 0.4, 6]} />
              <meshStandardMaterial color="#2c3e50" metalness={1} roughness={0.1} />
            </mesh>
            {/* Engine glow when launching */}
            {isLaunching && (
              <mesh position={[x, -0.8, z]}>
                <sphereGeometry args={[0.25, 8, 8]} />
                <meshBasicMaterial color="#ff6b35" transparent opacity={0.8} />
              </mesh>
            )}
          </group>
        )
      })}

      {/* Fins - 4 around the rocket */}
      {[0, 90, 180, 270].map((angle, i) => {
        const rad = (angle * Math.PI) / 180
        const x = Math.cos(rad) * 0.5
        const z = Math.sin(rad) * 0.5
        return (
          <mesh key={i} position={[x, 0, z]} rotation={[0, rad, 0]}>
            <boxGeometry args={[0.05, 1.5, 0.8]} />
            <meshStandardMaterial color="#e74c3c" metalness={0.6} roughness={0.4} />
          </mesh>
        )
      })}

      {/* Launch Pad Base (only shown when not launching) */}
      {!isLaunching && (
        <group position={[0, -1.5, 0]}>
          <mesh>
            <cylinderGeometry args={[1.5, 1.5, 0.3, 16]} />
            <meshStandardMaterial color="#95a5a6" metalness={0.3} roughness={0.7} />
          </mesh>
          {/* Support pillars */}
          {[0, 90, 180, 270].map((angle, i) => {
            const rad = (angle * Math.PI) / 180
            const x = Math.cos(rad) * 1.2
            const z = Math.sin(rad) * 1.2
            return (
              <mesh key={i} position={[x, 0.5, z]}>
                <cylinderGeometry args={[0.1, 0.1, 1, 6]} />
                <meshStandardMaterial color="#7f8c8d" />
              </mesh>
            )
          })}
        </group>
      )}
    </group>
  )
}
