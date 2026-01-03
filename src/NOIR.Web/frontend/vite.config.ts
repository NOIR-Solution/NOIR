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
        target: 'http://localhost:5228',
        changeOrigin: true,
        // Enable WebSocket proxy for any real-time features
        ws: true,
        // Configure proxy to handle Scalar API docs properly
        configure: (proxy) => {
          proxy.on('proxyRes', (proxyRes) => {
            // Remove X-Frame-Options for development to avoid issues
            // with embedded API documentation viewers
            if (proxyRes.headers['x-frame-options']) {
              delete proxyRes.headers['x-frame-options']
            }
          })
        },
      },
      '/hangfire': {
        target: 'http://localhost:5228',
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
