import { useState, useEffect } from "react"
import { useTheme } from "next-themes"
import { Moon, Sun } from "lucide-react"
import { motion } from "framer-motion"

export default function Header() {
  const [mounted, setMounted] = useState(false)
  const { theme, setTheme } = useTheme()

  useEffect(() => setMounted(true), [])

  return (
    <header className="py-6">
      <div className="container mx-auto px-4 flex justify-between items-center">
        <motion.div initial={{ opacity: 0, x: -20 }} animate={{ opacity: 1, x: 0 }} transition={{ duration: 0.5 }}>
          <h1 className="text-2xl font-bold text-gray-800 dark:text-white">CMS Pro</h1>
        </motion.div>
        <nav>
          <ul className="flex space-x-4">
            <motion.li
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.5, delay: 0.1 }}
            >
              <a
                href="#features"
                className="text-gray-600 hover:text-gray-800 dark:text-gray-300 dark:hover:text-white"
              >
                Features
              </a>
            </motion.li>
            <motion.li
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.5, delay: 0.2 }}
            >
              <a
                href="#testimonials"
                className="text-gray-600 hover:text-gray-800 dark:text-gray-300 dark:hover:text-white"
              >
                Testimonials
              </a>
            </motion.li>
            <motion.li
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.5, delay: 0.3 }}
            >
              {mounted && (
                <button
                  onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
                  className="p-2 rounded-full bg-gray-200 dark:bg-gray-800"
                >
                  {theme === "dark" ? <Sun size={20} /> : <Moon size={20} />}
                </button>
              )}
            </motion.li>
          </ul>
        </nav>
      </div>
    </header>
  )
}

