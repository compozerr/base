import * as React from 'react'
import { createFileRoute, redirect } from '@tanstack/react-router'
import Hero from '@/components/hero'
import Features from '@/components/features'
import Footer from '@/components/footer'
import MouseMoveEffect from '@/components/mouse-move-effect'
import Navbar from '@/components/navbar'

export const Route = createFileRoute('/')({
  component: HomeComponent,
  beforeLoad({ context }) {
    if (context.auth.isAuthenticated) {
      throw redirect({
        to: '/projects',
      })
    }
  },
})

function HomeComponent() {
  return (
    <>
      <MouseMoveEffect />

      <div className="relative min-h-screen">
        {/* Background gradients */}
        <div className="pointer-events-none fixed inset-0">
          <div className="absolute inset-0 bg-gradient-to-b from-background via-background/90 to-background" />
          <div className="absolute right-0 top-0 h-[500px] w-[500px] bg-blue-500/10 blur-[100px]" />
          <div className="absolute bottom-0 left-0 h-[500px] w-[500px] bg-purple-500/10 blur-[100px]" />
        </div>

        <div className="relative z-10">
          <Navbar />
          <Hero />
          <Features />
          <div className='mb-64' />
          <Footer />
        </div>
      </div>
    </>
  )
}
