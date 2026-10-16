import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/e2e",
  use: {
    baseURL: process.env.BASE_URL ?? "http://localhost:5182",
    trace: "retain-on-failure"
  },
  webServer: [
    {
      command: "dotnet run --project src/afc-api",
      url: "http://localhost:5181/health",
      reuseExistingServer: !process.env.CI
    },
    {
      command: "dotnet run --project src/afc-web-host",
      url: "http://localhost:5182",
      reuseExistingServer: !process.env.CI
    }
  ]
});
