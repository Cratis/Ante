// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Builds the lobby's frontend into Source/Ante/wwwroot for the E2E host to serve.
//
// PrimeReact 11 needs a license key at runtime, and Cratis Components refuses to render (the lobby shows only its
// render recovery notice) unless the application attests that one is configured. The build therefore needs
// ANTE_PRIMEUI_LICENSE, from the environment or the repository root's .env, exactly like `yarn build` in CI.
//
// On a machine without the team's license, ANTE_E2E_ALLOW_UNLICENSED_PRIMEUI=1 builds with a placeholder key instead:
// PrimeReact then shows its own "Invalid PrimeUI License" banner in every page. That build is for running the suite
// locally only; it is not the licensed build CI verifies, and CI never sets the variable.

import { spawnSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const licenseVariable = 'ANTE_PRIMEUI_LICENSE';
const envFile = fileURLToPath(new URL('../.env', import.meta.url));
const fromEnvFile = existsSync(envFile) && readFileSync(envFile, 'utf8').split(/\r?\n/).some(line => line.startsWith(`${licenseVariable}=`) && line.length > licenseVariable.length + 1);
const environment = { ...process.env };

if (!environment[licenseVariable] && !fromEnvFile) {
    if (environment.ANTE_E2E_ALLOW_UNLICENSED_PRIMEUI !== '1') {
        console.error(`${licenseVariable} is not set (nor in .env at the repository root). The lobby does not render without it.`);
        console.error('Set it to the team\'s PrimeUI license, or set ANTE_E2E_ALLOW_UNLICENSED_PRIMEUI=1 to run locally with PrimeUI\'s invalid-license banner.');
        process.exit(1);
    }

    console.warn(`Building without a PrimeUI license: every page shows PrimeUI's "Invalid PrimeUI License" banner. This is not the licensed build CI verifies.`);
    environment[licenseVariable] = 'unlicensed-local-e2e';
}

const result = spawnSync('yarn', ['workspace', 'ante', 'build'], { stdio: 'inherit', env: environment });
process.exit(result.status ?? 1);
