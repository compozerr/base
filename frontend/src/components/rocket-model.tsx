import { useRef, useMemo } from 'react'
import { useFrame } from '@react-three/fiber'
import * as THREE from 'three'
import { MeshTransmissionMaterial, Sparkles } from '@react-three/drei'

interface RocketModelProps {
  position?: [number, number, number]
  isLaunching?: boolean
  launchProgress?: number
  ignitionActive?: boolean
}

export function RocketModel({
  position = [0, 0, 0],
  isLaunching = false,
  launchProgress = 0,
  ignitionActive = false
}: RocketModelProps) {
  const rocketRef = useRef<THREE.Group>(null!)
  const engineGlowRef = useRef<THREE.PointLight>(null!)
  const exhaustRef = useRef<THREE.Group>(null!)

  // Create custom materials
  const bodyMaterial = useMemo(() => new THREE.MeshStandardMaterial({
    color: '#f8f9fa',
    metalness: 0.9,
    roughness: 0.1,
    envMapIntensity: 1.5,
  }), [])

  const carbonFiberMaterial = useMemo(() => new THREE.MeshStandardMaterial({
    color: '#1a1a1a',
    metalness: 0.6,
    roughness: 0.4,
    normalScale: new THREE.Vector2(0.5, 0.5),
  }), [])

  const engineMaterial = useMemo(() => new THREE.MeshStandardMaterial({
    color: '#0a0a0a',
    metalness: 1.0,
    roughness: 0.05,
  }), [])

  const accentMaterial = useMemo(() => new THREE.MeshStandardMaterial({
    color: '#e63946',
    metalness: 0.7,
    roughness: 0.2,
    emissive: '#e63946',
    emissiveIntensity: 0.2,
  }), [])

  // Animate rocket vibration and rotation
  useFrame((state) => {
    if (rocketRef.current) {
      if (ignitionActive && !isLaunching) {
        // Intense pre-launch vibration
        const time = state.clock.getElapsedTime()
        rocketRef.current.rotation.z = Math.sin(time * 25) * 0.03
        rocketRef.current.rotation.x = Math.cos(time * 20) * 0.02
        rocketRef.current.position.y = position[1] + Math.sin(time * 30) * 0.05
      } else if (isLaunching) {
        // Launch vibration - more chaotic
        rocketRef.current.rotation.z = (Math.random() - 0.5) * 0.08
        rocketRef.current.rotation.x = (Math.random() - 0.5) * 0.05
      } else {
        // Subtle idle sway
        const time = state.clock.getElapsedTime()
        rocketRef.current.rotation.z = Math.sin(time * 2) * 0.01
      }
    }

    // Engine glow intensity
    if (engineGlowRef.current) {
      const baseIntensity = ignitionActive ? 15 : 0
      const flickerIntensity = isLaunching ? 25 : baseIntensity
      engineGlowRef.current.intensity = flickerIntensity + Math.random() * 5
    }

    // Exhaust plume rotation
    if (exhaustRef.current && isLaunching) {
      exhaustRef.current.rotation.y += 0.05
    }
  })

  return (
    <group ref={rocketRef} position={position}>
      {/* Nose Cone - Sleek pointed design */}
      <mesh position={[0, 4.5, 0]} castShadow>
        <coneGeometry args={[0.4, 1.8, 32]} />
        <primitive object={carbonFiberMaterial} attach="material" />
      </mesh>

      {/* Capsule Window */}
      <mesh position={[0, 4.2, 0.41]} rotation={[Math.PI / 2, 0, 0]}>
        <circleGeometry args={[0.15, 32]} />
        <meshStandardMaterial
          color="#1e90ff"
          metalness={1}
          roughness={0}
          emissive="#1e90ff"
          emissiveIntensity={0.5}
          transparent
          opacity={0.8}
        />
      </mesh>

      {/* Upper Body Section */}
      <mesh position={[0, 3.5, 0]} castShadow>
        <cylinderGeometry args={[0.4, 0.5, 1.2, 32]} />
        <primitive object={bodyMaterial} attach="material" />
      </mesh>

      {/* Main Body - White with carbon fiber bands */}
      <mesh position={[0, 2.2, 0]} castShadow>
        <cylinderGeometry args={[0.5, 0.5, 2.6, 32]} />
        <primitive object={bodyMaterial} attach="material" />
      </mesh>

      {/* Carbon Fiber Bands */}
      {[3.2, 2.5, 1.8].map((y, i) => (
        <mesh key={i} position={[0, y, 0]} castShadow>
          <cylinderGeometry args={[0.51, 0.51, 0.15, 32]} />
          <primitive object={carbonFiberMaterial} attach="material" />
        </mesh>
      ))}

      {/* Accent Stripes */}
      {[3.0, 2.0].map((y, i) => (
        <mesh key={i} position={[0, y, 0]} castShadow>
          <cylinderGeometry args={[0.515, 0.515, 0.08, 32]} />
          <primitive object={accentMaterial} attach="material" />
        </mesh>
      ))}

      {/* Payload Section */}
      <mesh position={[0, 0.7, 0]} castShadow>
        <cylinderGeometry args={[0.5, 0.55, 0.8, 32]} />
        <primitive object={carbonFiberMaterial} attach="material" />
      </mesh>

      {/* Engine Section - Tapered */}
      <mesh position={[0, 0.1, 0]} castShadow>
        <cylinderGeometry args={[0.55, 0.65, 0.6, 32]} />
        <primitive object={engineMaterial} attach="material" />
      </mesh>

      {/* Grid Fins - 4 modern grid fins */}
      {[0, 90, 180, 270].map((angle, i) => {
        const rad = (angle * Math.PI) / 180
        const x = Math.cos(rad) * 0.5
        const z = Math.sin(rad) * 0.5
        return (
          <group key={i} position={[x, 1.2, z]} rotation={[0, rad, 0]}>
            {/* Fin base */}
            <mesh castShadow>
              <boxGeometry args={[0.05, 1.2, 0.6]} />
              <primitive object={carbonFiberMaterial} attach="material" />
            </mesh>
            {/* Grid pattern */}
            {[0.15, -0.15].map((zOffset, j) => (
              <mesh key={j} position={[0, 0, zOffset]} castShadow>
                <boxGeometry args={[0.06, 1.2, 0.03]} />
                <primitive object={accentMaterial} attach="material" />
              </mesh>
            ))}
          </group>
        )
      })}

      {/* Landing Legs - Modern telescoping design */}
      {!isLaunching && [0, 120, 240].map((angle, i) => {
        const rad = (angle * Math.PI) / 180
        const x = Math.cos(rad) * 0.45
        const z = Math.sin(rad) * 0.45
        return (
          <group key={i} position={[x, -0.2, z]} rotation={[0, rad, Math.PI / 6]}>
            {/* Upper leg */}
            <mesh position={[0, 0, 0.3]} castShadow>
              <cylinderGeometry args={[0.04, 0.035, 0.8, 8]} />
              <primitive object={carbonFiberMaterial} attach="material" />
            </mesh>
            {/* Lower leg */}
            <mesh position={[0, -0.25, 0.65]} castShadow>
              <cylinderGeometry args={[0.035, 0.03, 0.5, 8]} />
              <primitive object={carbonFiberMaterial} attach="material" />
            </mesh>
            {/* Foot pad */}
            <mesh position={[0, -0.5, 0.9]} castShadow>
              <cylinderGeometry args={[0.12, 0.12, 0.05, 16]} />
              <primitive object={engineMaterial} attach="material" />
            </mesh>
          </group>
        )
      })}

      {/* Main Engine Cluster - 9 engines in SpaceX style */}
      {[
        [0, 0], // Center
        [0.25, 0], [0, 0.25], [-0.25, 0], [0, -0.25], // Inner ring
        [0.35, 0.35], [-0.35, 0.35], [-0.35, -0.35], [0.35, -0.35] // Outer ring
      ].map(([xOffset, zOffset], i) => (
        <group key={i} position={[xOffset, -0.4, zOffset]}>
          {/* Engine bell */}
          <mesh castShadow>
            <cylinderGeometry args={[0.08, 0.11, 0.35, 16]} />
            <primitive object={engineMaterial} attach="material" />
          </mesh>
          {/* Inner nozzle */}
          <mesh position={[0, -0.05, 0]}>
            <cylinderGeometry args={[0.06, 0.08, 0.25, 16]} />
            <meshStandardMaterial
              color="#2d1810"
              metalness={0.5}
              roughness={0.6}
            />
          </mesh>

          {/* Engine glow when active */}
          {(ignitionActive || isLaunching) && (
            <>
              <mesh position={[0, -0.2, 0]}>
                <sphereGeometry args={[0.12, 16, 16]} />
                <meshStandardMaterial
                  color="#ff6b35"
                  emissive="#ff4500"
                  emissiveIntensity={2}
                  transparent
                  opacity={0.9}
                />
              </mesh>
              {/* Point light for engine */}
              <pointLight
                position={[0, -0.3, 0]}
                intensity={5}
                color="#ff6b35"
                distance={3}
                decay={2}
              />
            </>
          )}
        </group>
      ))}

      {/* Main engine glow point light */}
      <pointLight
        ref={engineGlowRef}
        position={[0, -0.5, 0]}
        color="#ff6b35"
        intensity={0}
        distance={10}
        decay={2}
      />

      {/* Exhaust Plume when launching */}
      {isLaunching && (
        <group ref={exhaustRef} position={[0, -0.7, 0]}>
          {/* Primary exhaust cone */}
          <mesh rotation={[Math.PI, 0, 0]}>
            <coneGeometry args={[0.8, 2.5, 32, 1, true]} />
            <meshStandardMaterial
              color="#ff6b35"
              emissive="#ff4500"
              emissiveIntensity={1.5}
              transparent
              opacity={0.7}
              side={THREE.DoubleSide}
            />
          </mesh>
          {/* Secondary exhaust glow */}
          <mesh rotation={[Math.PI, 0, 0]} position={[0, -0.5, 0]}>
            <coneGeometry args={[1.2, 3.5, 32, 1, true]} />
            <meshStandardMaterial
              color="#ffaa00"
              emissive="#ff8c00"
              emissiveIntensity={1}
              transparent
              opacity={0.4}
              side={THREE.DoubleSide}
            />
          </mesh>
          {/* Outer shock diamonds effect */}
          <mesh rotation={[Math.PI, 0, 0]} position={[0, -1, 0]}>
            <coneGeometry args={[1.5, 4, 32, 1, true]} />
            <meshStandardMaterial
              color="#ffffff"
              emissive="#ffd700"
              emissiveIntensity={0.8}
              transparent
              opacity={0.2}
              side={THREE.DoubleSide}
            />
          </mesh>
        </group>
      )}

      {/* Sparkles for magic effect */}
      {(ignitionActive || isLaunching) && (
        <Sparkles
          count={50}
          scale={2}
          size={3}
          speed={0.6}
          opacity={0.8}
          color="#ff6b35"
          position={[0, -0.5, 0]}
        />
      )}

      {/* Launch Pad (only when not launching) */}
      {!isLaunching && (
        <group position={[0, -1.5, 0]}>
          {/* Main platform */}
          <mesh receiveShadow>
            <cylinderGeometry args={[2, 2, 0.3, 32]} />
            <meshStandardMaterial
              color="#34495e"
              metalness={0.5}
              roughness={0.5}
            />
          </mesh>

          {/* Platform details - hexagonal pattern */}
          {Array.from({ length: 6 }).map((_, i) => {
            const angle = (i * Math.PI * 2) / 6
            const x = Math.cos(angle) * 1.5
            const z = Math.sin(angle) * 1.5
            return (
              <mesh key={i} position={[x, 0.16, z]} receiveShadow>
                <cylinderGeometry args={[0.3, 0.3, 0.02, 6]} />
                <meshStandardMaterial color="#e63946" metalness={0.8} roughness={0.2} />
              </mesh>
            )
          })}

          {/* Support towers */}
          {[0, 90, 180, 270].map((angle, i) => {
            const rad = (angle * Math.PI) / 180
            const x = Math.cos(rad) * 1.6
            const z = Math.sin(rad) * 1.6
            return (
              <group key={i} position={[x, 0, z]}>
                {/* Main pillar */}
                <mesh castShadow>
                  <cylinderGeometry args={[0.08, 0.08, 3, 16]} />
                  <primitive object={engineMaterial} attach="material" />
                </mesh>
                {/* Top platform */}
                <mesh position={[0, 1.5, 0]} castShadow>
                  <boxGeometry args={[0.15, 0.05, 0.15]} />
                  <primitive object={accentMaterial} attach="material" />
                </mesh>
                {/* Accent rings */}
                {[0.5, 1.0].map((y, j) => (
                  <mesh key={j} position={[0, y, 0]} castShadow>
                    <cylinderGeometry args={[0.1, 0.1, 0.05, 16]} />
                    <primitive object={accentMaterial} attach="material" />
                  </mesh>
                ))}
              </group>
            )
          })}

          {/* Umbilical connectors */}
          {[45, 135, 225, 315].map((angle, i) => {
            const rad = (angle * Math.PI) / 180
            const x = Math.cos(rad) * 0.6
            const z = Math.sin(rad) * 0.6
            return (
              <mesh key={i} position={[x, 0.8, z]} rotation={[0, rad, Math.PI / 2]} castShadow>
                <cylinderGeometry args={[0.03, 0.03, 0.5, 8]} />
                <meshStandardMaterial color="#95a5a6" metalness={0.7} roughness={0.3} />
              </mesh>
            )
          })}
        </group>
      )}
    </group>
  )
}
