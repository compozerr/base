import { createFileRoute } from '@tanstack/react-router'
import ExampleComponent from '@repo/template/template-component'

export const Route = createFileRoute('/using-module-component')({
  component: RouteComponent,
})

function RouteComponent() {
  return (
    <div>
      <h3>Using Module Component</h3>
      <ExampleComponent name="World!" />
    </div>
  )
}
