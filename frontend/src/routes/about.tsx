import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { apiClient } from '../api-client';

export const Route = createFileRoute('/about')({
  component: AboutComponent,
})

function AboutComponent() {
  apiClient.v1.example.get().then((response) => {
    console.log(response?.message)
  });

  return (
    <div>
      <h3>About</h3>
      <h1>{import.meta.env.VITE_SOMETHING}</h1>
    </div>
  )
}
