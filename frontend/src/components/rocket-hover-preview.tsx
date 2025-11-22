import { Canvas } from '@react-three/fiber'
import { PerspectiveCamera } from '@react-three/drei'
import { RocketModel } from './rocket-model'
import { motion } from 'framer-motion'

interface RocketHoverPreviewProps {
  isHovered: boolean
}

export function RocketHoverPreview({ isHovered }: RocketHoverPreviewProps) {
  return (
    <motion.div
      initial={{ y: 0 }}
      animate={{ y: isHovered ? -30 : 0 }} // Slide up 30px on hover
      transition={{ type: 'spring', stiffness: 300, damping: 30 }}
      className="fixed left-0 right-0 z-40"
      style={{
        bottom: 0,
        height: '100vh',
        pointerEvents: 'none', // Pass all mouse events through
        // Use transform to position mostly off screen
        transform: 'translateY(85vh)', // Push 85% down, leaving 15% visible
      }}
    >
      <Canvas style={{ background: 'transparent', pointerEvents: 'none' }}>
        {/* Same camera as launch sequence */}
        <PerspectiveCamera makeDefault position={[0, 3, 8]} fov={60} />

        {/* Same lighting as launch sequence */}
        <ambientLight intensity={0.4} />
        <pointLight position={[3, 5, 3]} intensity={0.3} />

        {/* Rocket at bottom position - tip should be visible */}
        <RocketModel
          position={[0, -8, 0]}
          vibrationIntensity={0}
          engineGlow={isHovered ? 2 : 0}
        />

        {/* No stars on hover - they fade in on click */}
      </Canvas>
    </motion.div>
  )
}
