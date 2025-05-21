import React from "react"
import { createFileRoute } from '@tanstack/react-router'
import ExampleComponent from '../../example-component'

export const Route = createFileRoute('/example/')({
  component: RouteComponent,
})

function RouteComponent() {
  return (
    <div>
      <ExampleComponent name="World!" />
    </div>
  )
}
