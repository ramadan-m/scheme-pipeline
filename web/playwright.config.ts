import { defineConfig } from '@playwright/test'

// The e2e run gets its own API (port 5081), Vite (5174) and database, so it never touches dev data.
// Needs Mongo running: `docker compose up -d` from the repo root.
const database = 'mongodb://localhost:27017/scheme_pipeline_e2e'

export default defineConfig({
  testDir: './e2e',
  testMatch: '*.e2e.ts',
  globalSetup: './e2e/reset-database.ts',
  use: { baseURL: 'http://localhost:5174', channel: 'chrome' },
  webServer: [
    {
      command: 'dotnet run --project ../api/SchemePipeline.Api --no-launch-profile --urls http://localhost:5081',
      url: 'http://localhost:5081/health',
      env: { ConnectionStrings__Mongo: database },
      timeout: 120_000,
    },
    {
      command: 'npx vite --port 5174 --strictPort',
      url: 'http://localhost:5174',
      env: { API_URL: 'http://localhost:5081' },
    },
  ],
})
