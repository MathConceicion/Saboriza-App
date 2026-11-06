import react from "@vitejs/plugin-react"
import path from "path"
import { defineConfig } from "vite"

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve("./src"),
    },
  },
  server: {
    port: 5173,
    proxy: {
      "/api": "http://localhost:5178",
    },
  },
  build: {
    outDir: "dist",
    sourcemap: false,
  },
})
