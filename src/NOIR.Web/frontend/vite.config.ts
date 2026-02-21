import { defineConfig, type Plugin } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

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

// https://vite.dev/config/
export default defineConfig({
  plugins: [suppressDevToolsShimWarning(), react(), tailwindcss()],
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
            if (id.includes('tinymce')) {
              return 'vendor-tinymce'
            }
            if (id.includes('recharts') || id.includes('d3-')) {
              return 'vendor-recharts'
            }
            if (id.includes('framer-motion')) {
              return 'vendor-framer'
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
    // Dev server port (3000 for Vibe Kanban compatibility)
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
