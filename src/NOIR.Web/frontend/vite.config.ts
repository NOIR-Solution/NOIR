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
    // Dev server port
    port: 5173,
    // Proxy API requests to .NET backend
    proxy: {
      '/api': {
        target: 'http://localhost:5228',
        changeOrigin: true,
      },
      '/hangfire': {
        target: 'http://localhost:5228',
        changeOrigin: true,
      },
    },
  },
})
