import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { AuthApi } from '../../../../../frontend/src/generated'

export const Route = createFileRoute('/login')({
  component: RouteComponent,
})

function RouteComponent() {
  const route = AuthApi.getV1authlogin();
  return 'Hello /login!'
}
