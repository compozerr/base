import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import Header from '@/components/header'
import Hero from '@/components/hero'
import Features from '@/components/features'
import Footer from '@/components/footer'
import Testimonials from '@/components/testimonials'

export const Route = createFileRoute('/')({
  component: HomeComponent,
})

function HomeComponent() {
  return (
    <div className="min-h-screen bg-white dark:bg-gray-900">
      <Header />
      <main>
        <Hero />
        <Features />
        <Testimonials />
      </main>
      <Footer />
    </div>
  )
}
