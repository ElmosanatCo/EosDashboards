import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";
import packageJson from "./package.json" with { type: "json" };

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  base: process.env.VITE_PUBLIC_BASE ?? "/",
  server: {
    proxy: {
      "/api": {
        target: "https://localhost",
        changeOrigin: true,
        secure: false,
        rewrite: (path) => `/EosDashboardsApi${path}`,
        configure: (proxy) => {
          proxy.on("proxyReq", (proxyRequest) => {
            proxyRequest.setHeader("origin", "https://localhost");
          });
        },
      },
    },
  },
  define: { __APP_VERSION__: JSON.stringify(packageJson.version) },
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    exclude: ["tests/e2e/**", "**/node_modules/**", "**/dist/**"],
  },
});
