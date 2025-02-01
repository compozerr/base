import { motion } from "framer-motion"
import { FileText, Users, Globe } from "lucide-react"

const features = [
  {
    icon: <FileText size={40} />,
    title: "Easy Content Creation",
    description: "Intuitive interface for creating and managing your content",
  },
  {
    icon: <Users size={40} />,
    title: "Team Collaboration",
    description: "Work together seamlessly with built-in collaboration tools",
  },
  {
    icon: <Globe size={40} />,
    title: "Multi-channel Publishing",
    description: "Publish your content across various platforms with ease",
  },
]

export default function Features() {
  return (
    <section id="features" className="py-20 bg-gray-100 dark:bg-gray-800">
      <div className="container mx-auto px-4">
        <h2 className="text-3xl font-bold text-center text-gray-800 dark:text-white mb-12">Features</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {features.map((feature, index) => (
            <motion.div
              key={index}
              className="bg-white dark:bg-gray-700 rounded-lg p-6 shadow-md"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.5, delay: index * 0.2 }}
            >
              <div className="text-primary mb-4">{feature.icon}</div>
              <h3 className="text-xl font-semibold text-gray-800 dark:text-white mb-2">{feature.title}</h3>
              <p className="text-gray-600 dark:text-gray-300">{feature.description}</p>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  )
}

