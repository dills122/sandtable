import { defineConfig, devices } from '@playwright/test';

/** Isolated browser check: mock API on 4301, leaving the user's 4200 preview untouched. */
export default defineConfig({
  testDir: './e2e',
  testMatch: 'connected-read.spec.ts',
  use: { ...devices['Desktop Chrome'], baseURL: 'http://127.0.0.1:4301' },
  reporter: 'list',
  webServer: {
    command: 'npx ng serve maproom-alpha --host 127.0.0.1 --port 4301',
    url: 'http://127.0.0.1:4301/connected',
    reuseExistingServer: false,
    timeout: 120000,
  },
});
