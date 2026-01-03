import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: {
    // Output to wwwroot folder for C# to serve
    outDir: '../wwwroot',
    emptyOutDir: true,
    rollupOptions: {
      output: {
        // Use hashed filenames for cache busting
        entryFileNames: 'assets/[name].[hash].js',
        chunkFileNames: 'assets/[name].[hash].js',
        assetFileNames: 'assets/[name].[hash].[ext]',
      },
    },
  },
  server: {
    // Dev server port (3000 for Vibe Kanban compatibility)
    port: 3000,
    strictPort: true, // Fail if port 3000 is in use (don't auto-switch)
    // Proxy API requests to .NET backend
    proxy: {
      '/api': {
        target: 'http://localhost:4000',
        changeOrigin: true,
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
        ws: true,
      },
    },
  },
})
