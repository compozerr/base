import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { TanStackRouterVite } from '@tanstack/router-plugin/vite'
import { getVirtualRouteConfig } from './virtual-route-config'
import path from "path"
import { execSync } from "child_process"

function getAppVersion(): string {
  try {
    return execSync('git describe --tags --abbrev=0').toString().trim()
  } catch {
    return process.env.VITE_APP_VERSION || 'dev'
  }
}

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [TanStackRouterVite({
    virtualRouteConfig: getVirtualRouteConfig(),
    routeFileIgnorePrefix: "!",
  }), react()],
  define: {
    __APP_VERSION__: JSON.stringify(getAppVersion()),
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server:{
    allowedHosts: true
  }
})
