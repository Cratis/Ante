// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig, devices } from '@playwright/test';
import { controlUrl } from './support/harness';

const isCI = Boolean(process.env.CI);

/**
 * The lobby's journeys in a real browser against a real Ante and Chronicle kernel. Playwright starts the E2E host
 * (Host/), which serves the frontend built into Source/Ante/wwwroot - run `yarn e2e` to build it first.
 */
export default defineConfig({
    testDir: './journeys',
    fullyParallel: true,
    forbidOnly: isCI,
    retries: 0,
    workers: isCI ? 2 : 4,
    timeout: 120_000,
    expect: { timeout: 15_000 },
    reporter: isCI ? [['list'], ['github'], ['html', { open: 'never', outputFolder: 'playwright-report' }]] : [['list']],
    outputDir: 'test-results',
    use: {
        trace: 'retain-on-failure',
        screenshot: 'only-on-failure',
        video: 'off'
    },
    projects: [
        // Forced colors emulation is Chromium's; one engine keeps the nightly run bounded.
        { name: 'chromium', use: { ...devices['Desktop Chrome'] } }
    ],
    webServer: {
        command: 'dotnet run --project Host/Ante.E2E.Host.csproj',
        url: `${controlUrl}/ready`,
        reuseExistingServer: !isCI,
        timeout: 300_000,
        stdout: 'pipe',
        stderr: 'pipe',
        gracefulShutdown: { signal: 'SIGTERM', timeout: 30_000 }
    }
});
