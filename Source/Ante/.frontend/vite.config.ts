/// <reference types="vitest/config" />

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';
import { EmitMetadataPlugin } from '@cratis/arc.vite';

export default defineConfig({
    root: fileURLToPath(new URL('./', import.meta.url)),
    // ANTE_ is this application's own namespace; PRIMEUI_ is PrimeTek's, so their license key keeps
    // the name it has everywhere else rather than a per-application copy.
    envPrefix: 'ANTE_',
    // One .env at the repository root serves the application; Vite otherwise only reads .env files
    // from this config's own directory.
    envDir: fileURLToPath(new URL('../../..', import.meta.url)),
    optimizeDeps: {
        exclude: ['tslib'],
    },
    build: {
        outDir: '../wwwroot',
        modulePreload: false,
        target: 'esnext',
        minify: false,
        cssCodeSplit: false,
    },
    test: {
        globals: true,
        environment: 'node',
        isolate: false,
        fileParallelism: false,
        pool: 'threads',
        coverage: {
            provider: 'v8',
            exclude: [
                '**/for_*/**',
                '**/wwwroot/**',
                '**/dist/**',
                '**/*.test.tsx',
                '**/*.d.ts',
                '**/declarations.ts',
            ],
        },
        exclude: ['../dist/**', '../node_modules/**', 'node_modules/**', '../wwwroot/**', 'wwwroot/**', '../**/given/**'],
        include: ['../**/for_*/when_*/**/*.ts', '../**/for_*/**/when_*.ts'],
        passWithNoTests: true,
        setupFiles: fileURLToPath(new URL('./vitest.setup.ts', import.meta.url))
    },
    plugins: [
        react(),
        EmitMetadataPlugin({ tsconfigPath: fileURLToPath(new URL('./tsconfig.json', import.meta.url)) }) as never
    ],
    server: {
        port: 9002,
        host: true,
        open: false,
        allowedHosts: ['host.docker.internal', 'aspire.dev.internal'],
        proxy: {
            '/.cratis': {
                target: 'http://localhost:5002',
                ws: true
            },
            '/api': {
                target: 'http://localhost:5002',
                ws: true
            },
            '/scalar': {
                target: 'http://localhost:5002'
            },
            '/openapi': {
                target: 'http://localhost:5000'
            }
        }
    },
    resolve: {
        alias: {
            'Strings': fileURLToPath(new URL('../Locales/Strings.ts', import.meta.url)),
            'given': fileURLToPath(new URL('../given.ts', import.meta.url)),
        }
    }
});
