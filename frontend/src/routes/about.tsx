import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { ExampleApi } from '../generated'

export const Route = createFileRoute('/about')({
  component: AboutComponent,
})

function AboutComponent() {
  ExampleApi.GetExample({ queries: { name: 'World' } }).then((response) => {
    console.log(response.message)
  });
  
  return (
    <div className="p-2">
      <h3>About</h3>
      <h1>{import.meta.env.VITE_SOMETHING}</h1>
    </div>
  )
}
