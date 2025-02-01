import { useState } from "react"
import { Check, Copy } from "lucide-react"
import { motion } from "framer-motion"

export function CopyCommand() {
  const [copied, setCopied] = useState(false)
  const command = `/bin/bash -c "$(curl -fsSL https://compozerr.com/install.sh)"`

  const copyToClipboard = async () => {
    await navigator.clipboard.writeText(command)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div className="mx-auto mt-6 max-w-3xl">
      <div className="relative flex items-center rounded-lg bg-muted/50 px-4 py-3 font-mono text-sm">
        <span className="mr-2 text-muted-foreground">$</span>
        <span className="text-foreground">{command}</span>
        <motion.button
          whileTap={{ scale: 0.95 }}
          onClick={copyToClipboard}
          className="absolute right-2 rounded-md p-2 hover:bg-muted"
        >
          <motion.div animate={copied ? { scale: [1, 0.8, 1] } : {}} transition={{ duration: 0.2 }}>
            {copied ? <Check className="h-4 w-4 text-green-500" /> : <Copy className="h-4 w-4 text-muted-foreground" />}
          </motion.div>
        </motion.button>
      </div>
    </div>
  )
}

