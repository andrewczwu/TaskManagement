import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Proxy /api to the backend so the frontend is same-origin (no CORS).
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5171',
    },
  },
})
