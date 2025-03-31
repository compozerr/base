import { useState } from "react"
import { Check, Copy } from "lucide-react"
import { motion } from "framer-motion"
import { cn } from "@/lib/utils"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"

export function CopyText({ value, className, children }: { value: string, className?: string, children?: React.ReactNode }) {
  const [copied, setCopied] = useState(false)

  const copyToClipboard = async () => {
    await navigator.clipboard.writeText(value)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <TooltipProvider>
      <Tooltip open={copied ? true : undefined}>
        <TooltipTrigger asChild>
          <div 
            onClick={copyToClipboard}
            className={cn("cursor-pointer", className)}
          >
            {children}
          </div>
        </TooltipTrigger>
        <TooltipContent>
          {copied ? "Copied!" : "Click to copy"}
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}

