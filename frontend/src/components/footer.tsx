import { Link } from "@tanstack/react-router"
import { useEffect, useState } from "react"
import { apiBaseUrl } from "@/api-client"

export default function Footer() {
  const frontendVersion = __APP_VERSION__
  const [backendVersion, setBackendVersion] = useState<string | null>(null)

  useEffect(() => {
    fetch(`${apiBaseUrl}/v1/version`)
      .then(res => res.json())
      .then(data => setBackendVersion(data.version))
      .catch(() => {})
  }, [])

  return (
    <footer className="border-t border-white/[0.06]">
      <div className="container max-w-6xl py-12 md:py-16">
        <div className="grid grid-cols-2 gap-8 md:grid-cols-4">
          {/* Brand */}
          <div className="col-span-2 md:col-span-1">
            <div className="flex items-center gap-2.5 mb-4">
              <div className="flex h-7 w-7 items-center justify-center rounded-md bg-primary/10 border border-primary/20">
                <span className="text-xs font-bold text-primary">C</span>
              </div>
              <span className="text-sm font-semibold tracking-tight">compozerr</span>
            </div>
            <p className="text-sm leading-relaxed text-muted-foreground max-w-[260px]">
              Independent hosting and modular templates for developers who value control.
            </p>
          </div>

          {/* Product */}
          <div>
            <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground/60 mb-4">Product</h3>
            <ul className="space-y-2.5">
              <li>
                <Link to="/n8n" className="text-sm text-muted-foreground transition-colors hover:text-foreground">
                  n8n Hosting
                </Link>
              </li>
              <li>
                <Link to="/pricing" className="text-sm text-muted-foreground transition-colors hover:text-foreground">
                  Pricing
                </Link>
              </li>
            </ul>
          </div>

          {/* Company */}
          <div>
            <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground/60 mb-4">Company</h3>
            <ul className="space-y-2.5">
              <li>
                <Link to="/contact" className="text-sm text-muted-foreground transition-colors hover:text-foreground">
                  Contact
                </Link>
              </li>
            </ul>
          </div>

          {/* Legal */}
          <div>
            <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground/60 mb-4">Legal</h3>
            <ul className="space-y-2.5">
              <li>
                <Link to="/terms-of-service" className="text-sm text-muted-foreground transition-colors hover:text-foreground">
                  Terms of Service
                </Link>
              </li>
              <li>
                <Link to="/privacy-policy" className="text-sm text-muted-foreground transition-colors hover:text-foreground">
                  Privacy Policy
                </Link>
              </li>
            </ul>
          </div>
        </div>
      </div>

      {/* Bottom bar */}
      <div className="container max-w-6xl border-t border-white/[0.06] py-6 flex flex-col sm:flex-row items-center justify-between gap-2">
        <p className="text-xs text-muted-foreground/50">
          &copy; {new Date().getFullYear()} compozerr. All rights reserved.
        </p>
        <p className="text-xs text-muted-foreground/30">
          {frontendVersion !== 'dev' && <>v{frontendVersion}</>}
          {frontendVersion !== 'dev' && backendVersion && backendVersion !== 'dev' && <> &middot; </>}
          {backendVersion && backendVersion !== 'dev' && <>api {backendVersion}</>}
        </p>
      </div>
    </footer>
  )
}
