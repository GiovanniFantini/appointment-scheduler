import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'
import { viteVersionPlugin } from './vite-version-plugin'

export default defineConfig({
  plugins: [react(), viteVersionPlugin()],
  resolve: {
    alias: {
      '@scheduler/ui': path.resolve(__dirname, '../shared-ui/src')
    }
  },
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true
      }
    }
  }
})
