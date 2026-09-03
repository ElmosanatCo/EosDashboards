import { defineConfig } from "@playwright/test";

const port = process.env.EOS_PLAYWRIGHT_PORT ?? "4173";
const baseURL = `http://127.0.0.1:${port}`;

export default defineConfig({
  testDir: "./tests/e2e",
  retries: 0,
  use: {
    baseURL,
    channel: "chrome",
    locale: "fa-IR",
    trace: "retain-on-failure",
  },
  webServer: {
    command: `npm run dev -- --host 127.0.0.1 --port ${port}`,
    url: baseURL,
    reuseExistingServer: false,
  },
});
