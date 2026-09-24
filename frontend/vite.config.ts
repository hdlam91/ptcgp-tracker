import { fileURLToPath, URL } from 'node:url'

import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    tailwindcss(),
    // Makes the app installable on a phone and lets the shell load offline. Disabled in `vite dev`
    // (no service worker while developing); use `npm run build && npm run preview`, or the Docker
    // image, to try it.
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg', 'apple-touch-icon-180x180.png'],
      manifest: {
        name: 'Pokémon TCG Pocket Collection Tracker',
        short_name: 'PTCGP Tracker',
        description: 'Track your Pokémon TCG Pocket collection, wishlist and trades.',
        id: '/',
        start_url: '/',
        scope: '/',
        display: 'standalone',
        theme_color: '#4338ca',
        background_color: '#4338ca',
        icons: [
          { src: 'pwa-192x192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512x512.png', sizes: '512x512', type: 'image/png' },
          { src: 'maskable-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        cleanupOutdatedCaches: true,
        // The bundled card dataset is one ~4 MB chunk, and Workbox skips files over 2 MB by default.
        maximumFileSizeToCacheInBytes: 8 * 1024 * 1024,
        navigateFallback: 'index.html',
        // API calls are per-user and must always hit the network; never answer them with the app shell.
        navigateFallbackDenylist: [/^\/api\//],
      },
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    proxy: {
      // Keeps the browser's view of frontend + backend same-origin in dev, so
      // cookie auth works without any CORS configuration (mirrors the nginx
      // reverse proxy used in the production image).
      '/api': {
        target: 'http://localhost:8080',
        changeOrigin: true,
      },
    },
  },
})
