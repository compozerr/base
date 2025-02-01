"use client"

import { motion } from "framer-motion"
import { Button } from "@/components/ui/button"

export default function Hero() {
  return (
    <section className="py-20 text-center">
      <div className="container mx-auto px-4">
        <motion.h2
          className="text-4xl md:text-6xl font-bold text-gray-800 dark:text-white mb-6"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8 }}
        >
          Manage Your Content with Ease
        </motion.h2>
        <motion.p
          className="text-xl text-gray-600 dark:text-gray-300 mb-8"
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, delay: 0.2 }}
        >
          Powerful, flexible, and user-friendly content management system
        </motion.p>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.8, delay: 0.4 }}
        >
          <Button size="lg">Get Started</Button>
        </motion.div>
      </div>
    </section>
  )
}

