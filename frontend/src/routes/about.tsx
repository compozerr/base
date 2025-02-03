import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { api } from '../api-client';

export const Route = createFileRoute('/about')({
  component: AboutComponent,
})

function AboutComponent() {
  const { data, isLoading } = api.v1.getExample.useQuery(
    { query: { name: "Hey" } }
  );

  return (
    <div>
      <h3>About</h3>
      <h1>{import.meta.env.VITE_SOMETHING}</h1>
      <p>{isLoading ? "loading..." : data?.message}</p>
    </div>
  )
}
