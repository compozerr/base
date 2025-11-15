import { Link } from "@tanstack/react-router"

export default function Footer() {
  return (
    <footer className="border-t">
      <div className="container flex flex-col gap-8 py-8 md:flex-row md:py-12">
        <div className="flex-1 space-y-4">
          <h2 className="font-bold">compozerr</h2>
          <p className="text-sm text-muted-foreground max-w-[400px]">
            Independent hosting for your automation workflows. Deploy n8n and other services without Big Tech dependencies.
          </p>
        </div>
        <div className="grid flex-1 grid-cols-2 gap-12">
          <div className="space-y-4">
            <h3 className="text-sm font-medium">Legal</h3>
            <ul className="space-y-3 text-sm">
              <li>
                <Link to="/contact" className="text-muted-foreground transition-colors hover:text-primary">
                  Contact
                </Link>
              </li>
              <li>
                <Link to="/terms-of-service" className="text-muted-foreground transition-colors hover:text-primary">
                  Terms of Service
                </Link>
              </li>
              <li>
                <Link to="/privacy-policy" className="text-muted-foreground transition-colors hover:text-primary">
                  Privacy Policy
                </Link>
              </li>
            </ul>
          </div>
          <div className="space-y-4">
            <h3 className="text-sm font-medium">Product</h3>
            <ul className="space-y-3 text-sm">
              <li>
                <Link to="/pricing" className="text-muted-foreground transition-colors hover:text-primary">
                  Pricing
                </Link>
              </li>
              <li>
                <Link to="/n8n" className="text-muted-foreground transition-colors hover:text-primary">
                  n8n Hosting
                </Link>
              </li>
            </ul>
          </div>
        </div>
      </div>
      <div className="container border-t py-6">
        <p className="text-center text-sm text-muted-foreground">
          © {new Date().getFullYear()} compozerr. All rights reserved.
        </p>
      </div>
    </footer>
  )
}

