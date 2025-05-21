import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { TanStackRouterVite } from '@tanstack/router-plugin/vite'
import { getVirtualRouteConfig } from './virtual-route-config'
import path from "path"

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [TanStackRouterVite({
    virtualRouteConfig: getVirtualRouteConfig(),
  }), react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  css: {
    postcss: "./postcss.config.js",
  },
  server: {
    allowedHosts: true
  }
})
