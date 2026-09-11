import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { visualizer } from "rollup-plugin-visualizer";

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    process.env.VISUALIZER === "true" &&
      visualizer({ open: false, gzipSize: true, brotliSize: true }),
  ].filter(Boolean),
  preview: {
    port: 5004,
  },
  server: {
    host: "127.0.0.1",
    port: 5004,
    proxy: {
      "/api": process.env.VITE_PROXY_TARGET! /* || "http://localhost:5005", */
    },
  },
});
