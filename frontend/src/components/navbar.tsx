import { Link } from "@tanstack/react-router"
import { Button } from "./ui/button"
import { Workflow } from "lucide-react"
import LoginButton from "./login-button"

export default function Navbar() {
  return (
    <header className="sticky top-0 z-50 w-full">
      <div className="container flex h-14 max-w-screen-2xl items-center">
        <Link to="/" className="mr-6 flex items-center space-x-2">
          <span className="font-bold">compozerr</span>
        </Link>
        <nav className="flex flex-1 items-center space-x-3 text-sm font-medium">
          <Link to="/n8n" className="transition-colors hover:text-primary">
            <Button variant="ghost" size="sm" className="gap-2">
              <Workflow className="h-4 w-4" />
              n8n
            </Button>
          </Link>
          <Link to="/pricing" className="transition-colors hover:text-primary">
            <Button variant="ghost" size="sm">
              Pricing
            </Button>
          </Link>
        </nav>
        <div className="flex items-center space-x-4">
          <LoginButton />
        </div>
      </div>
    </header>
  )
}

