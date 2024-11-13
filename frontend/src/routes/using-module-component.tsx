import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import ExampleComponent from '@template/example-component'

export const Route = createFileRoute('/using-module-component')({
  component: RouteComponent,
})

function RouteComponent() {
  return (
    <div>
      <h3>Using Module Component</h3>
      <ExampleComponent name='World!' />
    </div>
  )
}
