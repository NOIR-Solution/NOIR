import { defineConfig, type Plugin } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'
import path from 'path'
import { localeHmrPlugin } from './plugins/vite-locale-hmr'

/**
 * Suppresses the false-positive "Something has shimmed the React DevTools
 * global hook" warning from react-refresh. Triggered by Playwright CDP or
 * browser extensions that inject __REACT_DEVTOOLS_GLOBAL_HOOK__ with
 * isDisabled=true before React loads. Dev-only (react-refresh is not in prod).
 */
const suppressDevToolsShimWarning = (): Plugin => ({
  name: 'suppress-devtools-shim-warning',
  apply: 'serve',
  transform(code, id) {
    if (id === '/@react-refresh' && code.includes('hook.isDisabled')) {
      return code.replace('if (hook.isDisabled)', 'if (false)')
    }
  },
})

// Only enable PWA in main app build (not Storybook)
const isStorybook = process.argv.some(arg => arg.includes('storybook'))

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    suppressDevToolsShimWarning(),
    react(),
    tailwindcss(),
    localeHmrPlugin(),
    !isStorybook && VitePWA({
      registerType: 'prompt',
      includeAssets: ['favicon.ico', 'favicon.svg', 'icons/*.png'],
      workbox: {
        runtimeCaching: [
          {
            // API calls: NetworkFirst with 3s timeout
            urlPattern: /^https?:\/\/.*\/api\/.*/i,
            handler: 'NetworkFirst',
            options: {
              cacheName: 'noir-api-cache',
              networkTimeoutSeconds: 3,
              expiration: { maxEntries: 50, maxAgeSeconds: 300 },
            },
          },
          {
            // Images: CacheFirst with 30 day expiration
            urlPattern: /\.(png|jpg|jpeg|gif|svg|webp|avif)$/i,
            handler: 'CacheFirst',
            options: {
              cacheName: 'noir-images',
              expiration: { maxEntries: 200, maxAgeSeconds: 2592000 },
            },
          },
          {
            // JS/CSS: StaleWhileRevalidate
            urlPattern: /\.(js|css)$/i,
            handler: 'StaleWhileRevalidate',
            options: {
              cacheName: 'noir-assets',
              expiration: { maxEntries: 100, maxAgeSeconds: 604800 },
            },
          },
          {
            // Fonts: CacheFirst with 1 year
            urlPattern: /\.(woff|woff2|ttf|eot)$/i,
            handler: 'CacheFirst',
            options: {
              cacheName: 'noir-fonts',
              expiration: { maxEntries: 30, maxAgeSeconds: 31536000 },
            },
          },
        ],
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2}'],
        navigateFallbackDenylist: [/^\/api\//, /^\/hubs\//, /^\/hangfire\//],
      },
      manifest: false, // Use existing manifest.json in public/
    }),
  ].filter(Boolean),
  resolve: {
    alias: {
      '@uikit': path.resolve(__dirname, './src/uikit'),
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: {
    // Output to wwwroot folder for C# to serve
    outDir: '../wwwroot',
    emptyOutDir: true,
    chunkSizeWarningLimit: 300,
    rollupOptions: {
      output: {
        // Use hashed filenames for cache busting
        entryFileNames: 'assets/[name].[hash].js',
        chunkFileNames: 'assets/[name].[hash].js',
        assetFileNames: 'assets/[name].[hash].[ext]',
        manualChunks(id) {
          if (id.includes('node_modules')) {
            if (id.includes('recharts') || id.includes('d3-')) {
              return 'vendor-recharts'
            }
            if (id.includes('@tiptap') || id.includes('prosemirror')) {
              return 'vendor-tiptap'
            }
            if (id.includes('framer-motion')) {
              return 'vendor-framer'
            }
            if (id.includes('katex')) {
              return 'vendor-katex'
            }
            if (id.includes('shiki')) {
              return 'vendor-shiki'
            }
            if (id.includes('@radix-ui')) {
              return 'vendor-radix'
            }
            if (id.includes('react-dom') || (id.includes('/react/') && !id.includes('react-'))) {
              return 'vendor-react'
            }
          }
        },
      },
    },
  },
  server: {
    // Dev server port
    port: 3000,
    strictPort: true, // Fail if port 3000 is in use (don't auto-switch)
    // Proxy API requests to .NET backend (HTTPS to avoid redirect header loss)
    proxy: {
      '/api': {
        target: 'http://localhost:4000',
        changeOrigin: true,
        secure: false,
        // Follow redirects server-side so browser never sees cross-origin redirects
        // (prevents CORS preflight failures from HTTPS redirection)
        followRedirects: true,
        // Enable WebSocket proxy for any real-time features
        ws: true,
        // Configure proxy to handle Scalar API docs and security headers
        configure: (proxy) => {
          proxy.on('proxyRes', (proxyRes, req) => {
            // For API docs, remove security headers that block Scalar's CDN scripts
            // This is safe in development - production serves directly from backend
            if (req.url?.startsWith('/api/docs') || req.url?.startsWith('/api/openapi')) {
              delete proxyRes.headers['content-security-policy']
              delete proxyRes.headers['x-frame-options']
              delete proxyRes.headers['x-content-type-options']
            }
          })
        },
      },
      '/hangfire': {
        target: 'http://localhost:4000',
        changeOrigin: true,
        secure: false,
        ws: true,
      },
      '/hubs': {
        target: 'http://localhost:4000',
        changeOrigin: true,
        secure: false,
        ws: true,
      },
      // Media files (images, uploads)
      '/media': {
        target: 'http://localhost:4000',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
