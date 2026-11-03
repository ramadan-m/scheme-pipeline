import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// In dev, the browser only talks to Vite; Vite forwards API calls, so there is no cross-origin request.
const api = 'http://localhost:5080'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': api,
      '/health': api,
    },
  },
})
