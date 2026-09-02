import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/e2e",
  retries: 0,
  use: {
    baseURL: "http://127.0.0.1:4173",
    channel: "chrome",
    locale: "fa-IR",
    trace: "retain-on-failure",
  },
  webServer: {
    command: "npm run dev -- --host 127.0.0.1 --port 4173",
    url: "http://127.0.0.1:4173",
    reuseExistingServer: false,
  },
});
