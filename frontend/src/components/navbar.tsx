import { Link } from "@tanstack/react-router"
import { Button } from "./ui/button"
import { CompozerrLogo } from "./compozerr-logo"
import { Workflow, Menu, X } from "lucide-react"
import LoginButton from "./login-button"
import { useState } from "react"

export default function Navbar() {
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <header className="sticky top-0 z-50 w-full border-b border-white/[0.06] bg-background/60 backdrop-blur-xl backdrop-saturate-150">
      <div className="container flex h-16 max-w-6xl items-center justify-between">
        {/* Logo */}
        <Link to="/" className="flex items-center">
          <CompozerrLogo className="text-[15px]" />
        </Link>

        {/* Desktop nav */}
        <nav className="hidden md:flex items-center gap-1">
          <Link to="/n8n" className="group">
            <Button variant="ghost" size="sm" className="gap-2 text-muted-foreground hover:text-foreground text-[13px] font-medium">
              <Workflow className="h-3.5 w-3.5 text-primary/70 group-hover:text-primary transition-colors" />
              n8n Hosting
            </Button>
          </Link>
          <Link to="/pricing">
            <Button variant="ghost" size="sm" className="text-muted-foreground hover:text-foreground text-[13px] font-medium">
              Pricing
            </Button>
          </Link>
          <Link to="/contact">
            <Button variant="ghost" size="sm" className="text-muted-foreground hover:text-foreground text-[13px] font-medium">
              Contact
            </Button>
          </Link>
        </nav>

        {/* Desktop CTA */}
        <div className="hidden md:flex items-center gap-3">
          <LoginButton />
        </div>

        {/* Mobile menu button */}
        <button
          className="md:hidden flex items-center justify-center h-9 w-9 rounded-lg text-muted-foreground hover:text-foreground hover:bg-white/[0.04] transition-colors"
          onClick={() => setMobileOpen(!mobileOpen)}
        >
          {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
        </button>
      </div>

      {/* Mobile nav */}
      {mobileOpen && (
        <div className="md:hidden border-t border-white/[0.06] bg-background/95 backdrop-blur-xl">
          <nav className="container max-w-6xl flex flex-col gap-1 py-4">
            <Link to="/n8n" onClick={() => setMobileOpen(false)} className="flex items-center gap-2 px-3 py-2.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-white/[0.04] text-sm font-medium transition-colors">
              <Workflow className="h-4 w-4 text-primary/70" />
              n8n Hosting
            </Link>
            <Link to="/pricing" onClick={() => setMobileOpen(false)} className="px-3 py-2.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-white/[0.04] text-sm font-medium transition-colors">
              Pricing
            </Link>
            <Link to="/contact" onClick={() => setMobileOpen(false)} className="px-3 py-2.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-white/[0.04] text-sm font-medium transition-colors">
              Contact
            </Link>
            <div className="mt-2 px-3">
              <LoginButton />
            </div>
          </nav>
        </div>
      )}
    </header>
  )
}
