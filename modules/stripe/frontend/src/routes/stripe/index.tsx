import React from "react"
import { createFileRoute } from '@tanstack/react-router'
import StripeComponent from '../../stripe-component'

export const Route = createFileRoute('/stripe/')({
  component: RouteComponent,
})

function RouteComponent() {
  return (
    <div>
      <StripeComponent name="World!" />
    </div>
  )
}
