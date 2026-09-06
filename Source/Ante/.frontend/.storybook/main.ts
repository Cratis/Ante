// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { StorybookConfig } from '@storybook/react-vite';
import type { UserConfig as ViteConfig } from 'vite';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const config: StorybookConfig = {
    stories: [
        '../../Invitations/**/*.stories.@(js|jsx|ts|tsx)',
        '../../Legal/**/*.stories.@(js|jsx|ts|tsx)',
        '../../Organization/**/*.stories.@(js|jsx|ts|tsx)',
    ],
    addons: [],
    framework: {
        name: '@storybook/react-vite',
        options: {}
    },
    core: { builder: '@storybook/builder-vite' },
    async viteFinal(existingConfig: ViteConfig) {
        const cfg: ViteConfig = { ...existingConfig };
        delete cfg.root;
        cfg.server = { ...(cfg.server || {}), open: false } as unknown;
        // Storybook builds with its own Vite config, so it needs the same env exposure the app has:
        // the ANTE_ prefix for the license key, read from the repository-root .env.
        cfg.envPrefix = 'ANTE_';
        cfg.envDir = path.resolve(__dirname, '../../../..');

        const root = path.resolve(__dirname, '../..');
        const newAliases = [
            { find: 'given', replacement: path.join(root, 'given.ts') },
            { find: 'Strings', replacement: path.join(root, 'Locales/Strings.ts') },
        ];

        const existingAlias = cfg.resolve?.alias;
        const existingAliasArray = Array.isArray(existingAlias)
            ? existingAlias
            : Object.entries(existingAlias || {}).map(([find, replacement]) => ({ find, replacement }));

        cfg.resolve = {
            ...(cfg.resolve || {}),
            alias: [...existingAliasArray, ...newAliases],
        };

        return cfg;
    }
};

export default config;
