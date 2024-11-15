import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { ExampleApi } from '../generated';

export const Route = createFileRoute('/about')({
  component: AboutComponent,
})

function AboutComponent() {
  ExampleApi.getExample({
    queries: {
      name: 'World!'
    }
  }).then(res => {
    console.log({ res })
  });

  return (
    <div className="p-2">
      <h3>About</h3>
    </div>
  )
}
