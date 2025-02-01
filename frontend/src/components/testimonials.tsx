import { useState, useEffect } from "react"
import { motion, AnimatePresence } from "framer-motion"

const testimonials = [
  {
    name: "John Doe",
    role: "Marketing Manager",
    content: "CMS Pro has revolutionized our content management process. It's intuitive and powerful!",
  },
  {
    name: "Jane Smith",
    role: "Content Creator",
    content: "I love how easy it is to create and publish content with CMS Pro. It's a game-changer!",
  },
  {
    name: "Mike Johnson",
    role: "Web Developer",
    content: "The flexibility and extensibility of CMS Pro make it my go-to choice for client projects.",
  },
]

export default function Testimonials() {
  const [currentIndex, setCurrentIndex] = useState(0)

  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentIndex((prevIndex) => (prevIndex + 1) % testimonials.length)
    }, 5000)
    return () => clearInterval(timer)
  }, [])

  return (
    <section id="testimonials" className="py-20">
      <div className="container mx-auto px-4">
        <h2 className="text-3xl font-bold text-center text-gray-800 dark:text-white mb-12">What Our Clients Say</h2>
        <div className="relative h-64">
          <AnimatePresence initial={false}>
            <motion.div
              key={currentIndex}
              className="absolute w-full"
              initial={{ opacity: 0, x: 100 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -100 }}
              transition={{ duration: 0.5 }}
            >
              <div className="bg-white dark:bg-gray-700 rounded-lg p-6 shadow-md">
                <p className="text-gray-600 dark:text-gray-300 mb-4">{testimonials[currentIndex].content}</p>
                <p className="font-semibold text-gray-800 dark:text-white">{testimonials[currentIndex].name}</p>
                <p className="text-gray-500 dark:text-gray-400">{testimonials[currentIndex].role}</p>
              </div>
            </motion.div>
          </AnimatePresence>
        </div>
      </div>
    </section>
  )
}

