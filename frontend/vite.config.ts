import { defineConfig, loadEnv, type Plugin } from 'vite'
import { devtools } from '@tanstack/devtools-vite'
import react from '@vitejs/plugin-react'
import { TanStackRouterVite } from '@tanstack/router-plugin/vite'
import { getVirtualRouteConfig } from './virtual-route-config'
import path from "path"
import fs from "fs"
import { execSync } from "child_process"

const REQUIRED_ENV_VARS = ["VITE_BACKEND_URL"] as const

function validateEnv(mode: string) {
  const env = loadEnv(mode, process.cwd(), "")
  const missing = REQUIRED_ENV_VARS.filter((name) => !env[name])
  if (missing.length > 0) {
    throw new Error(
      `Missing required environment variable(s): ${missing.join(", ")}. ` +
      `Ensure frontend/.env exists and defines these before building. ` +
      `See frontend/.env.example for the expected values.`
    )
  }
}

function getAppVersion(): string {
  try {
    return execSync('git describe --tags --abbrev=0').toString().trim()
  } catch {
    return process.env.VITE_APP_VERSION || 'dev'
  }
}

function sitemapPlugin(): Plugin {
  const baseUrl = 'https://compozerr.com'
  const routes: { path: string; priority: string; changefreq: string }[] = [
    { path: '/', priority: '1.0', changefreq: 'weekly' },
    { path: '/n8n', priority: '0.9', changefreq: 'weekly' },
    { path: '/pricing', priority: '0.8', changefreq: 'weekly' },
    { path: '/contact', priority: '0.5', changefreq: 'monthly' },
    { path: '/privacy-policy', priority: '0.3', changefreq: 'monthly' },
    { path: '/terms-of-service', priority: '0.3', changefreq: 'monthly' },
  ]

  let outDir: string

  return {
    name: 'generate-sitemap',
    configResolved(config) {
      outDir = config.build.outDir
    },
    closeBundle() {
      const resolvedOutDir = path.resolve(outDir)
      fs.mkdirSync(resolvedOutDir, { recursive: true })
      const lastmod = new Date().toISOString().split('T')[0]
      const urls = routes
        .map(r => `  <url>\n    <loc>${baseUrl}${r.path}</loc>\n    <lastmod>${lastmod}</lastmod>\n    <changefreq>${r.changefreq}</changefreq>\n    <priority>${r.priority}</priority>\n  </url>`)
        .join('\n')
      const sitemap = `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls}\n</urlset>\n`
      fs.writeFileSync(path.join(resolvedOutDir, 'sitemap.xml'), sitemap)
    },
  }
}

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  validateEnv(mode)
  return {
    plugins: [devtools(), TanStackRouterVite({
      virtualRouteConfig: getVirtualRouteConfig(),
      routeFileIgnorePrefix: "!",
    }), react(), sitemapPlugin()],
    define: {
      __APP_VERSION__: JSON.stringify(getAppVersion()),
    },
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
    server: {
      allowedHosts: true
    }
  }
})
