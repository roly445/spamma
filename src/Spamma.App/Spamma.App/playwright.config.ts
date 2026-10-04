import { defineConfig, devices } from '@playwright/test';
import { defineBddConfig } from 'playwright-bdd';

const baseURL = process.env.SPAMMA_E2E_BASE_URL ?? 'http://127.0.0.1:5188';
const testDir = defineBddConfig({
  features: 'e2e/features/*.feature',
  steps: 'e2e/steps/*.ts',
});

export default defineConfig({
  testDir,
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'dotnet run --project Spamma.App.csproj --configuration Release --no-build --no-launch-profile',
    url: `${baseURL}/health`,
    timeout: 120_000,
    reuseExistingServer: !process.env.CI,
    env: {
      ASPNETCORE_URLS: baseURL,
      ASPNETCORE_ENVIRONMENT: 'Production',
      Setup__Completed: '2026-01-01T00:00:00Z',
      SmtpServer__Port: '2526',
    },
  },
});
