import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { TanStackRouterVite } from '@tanstack/router-plugin/vite'
import { getVirtualRouteConfig } from './virtual-route-config'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [TanStackRouterVite({
    virtualRouteConfig: getVirtualRouteConfig(),
  }), react()],
})
