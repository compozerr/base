import { useState } from "react"
import { Check, Copy } from "lucide-react"
import { motion } from "framer-motion"
import { cn } from "@/lib/utils"

export function CopyButton({ value, className, children }: { value: string, className?: string, children?: React.ReactNode }) {
  const [copied, setCopied] = useState(false)

  const copyToClipboard = async () => {
    await navigator.clipboard.writeText(value)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <motion.button
      whileTap={{ scale: 0.95 }}
      onClick={copyToClipboard}
      className={cn("rounded-md p-2 hover:bg-muted flex flex-row items-center", className)}
    >
      <motion.div animate={copied ? { scale: [1, 0.8, 1] } : {}} transition={{ duration: 0.2 }}>
        {copied ? <Check className="h-4 w-4 text-green-500" /> : <Copy className="h-4 w-4 text-muted-foreground" />}
      </motion.div>
      {children}
    </motion.button>
  )
}

