import { Button } from "@/components/ui/button"
import { ArrowRight } from "lucide-react"
import { CopyCommand } from "./copy-command"
import StyledLink from "./styled-link"

export default function Hero() {
  return (
    <section className="container flex min-h-[calc(100vh-3.5rem)] max-w-screen-2xl flex-col items-center justify-center space-y-8 py-24 text-center md:py-32">
      <div className="space-y-4">
        <h1 className="bg-gradient-to-br from-foreground from-30% via-foreground/90 to-foreground/70 bg-clip-text text-4xl font-bold tracking-tight text-transparent sm:text-5xl md:text-6xl lg:text-7xl">
          Innovate Faster with
          <br />
          compozerr
        </h1>
        <br/>
        <CopyCommand />
      </div>
      <div className="flex gap-4 pt-4">
        <StyledLink href="/docs" size="lg">
          Documentation
          <ArrowRight className="ml-2 h-4 w-4" />
        </StyledLink>
        <Button variant="outline" size="lg">
          Watch demo
        </Button>
      </div>
      <div className="mx-auto mt-16 max-w-[42rem]">
        <p className="leading-normal text-muted-foreground sm:text-xl sm:leading-8">
          Compozerr revolutionizes project initialization by offering full-stack, modular templates. Download precisely what you need - from authentication and database implementations to payment integrations. Each module comes complete with both frontend and backend code, allowing you to build production-ready applications faster than ever before.
        </p>
      </div>
    </section>
  )
}

