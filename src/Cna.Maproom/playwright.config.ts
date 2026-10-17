import { defineConfig, devices } from '@playwright/test';
const appPort = process.env['MAPROOM_E2E_PORT'] ?? '4200';
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  expect: { timeout: 15000 },
  forbidOnly: !!process.env['CI'],
  retries: 0,
  workers: 2,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    ...devices['Desktop Chrome'],
    baseURL: `http://127.0.0.1:${appPort}`,
    trace: 'retain-on-failure',
    ...(process.env['PLAYWRIGHT_CHANNEL'] ? { channel: process.env['PLAYWRIGHT_CHANNEL'] } : {}),
  },
  webServer: [
    {
      command: process.env['MAPROOM_E2E_PORT']
        ? `ng serve maproom-alpha --host 127.0.0.1 --port ${appPort}`
        : 'npm start',
      url: `http://127.0.0.1:${appPort}`,
      reuseExistingServer: !process.env['CI'],
      timeout: 120000,
    },
    {
      command: 'npm run storybook -- --ci',
      url: 'http://127.0.0.1:6006',
      reuseExistingServer: !process.env['CI'],
      timeout: 120000,
    },
  ],
});
