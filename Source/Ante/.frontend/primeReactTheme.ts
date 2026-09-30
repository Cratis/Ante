// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import Lara from '@primeuix/themes/lara';

/**
 * The theme Ante hands to `PrimeReactProvider`.
 *
 * Ante is a reusable lobby, not a branded product, so it starts from PrimeReact's own Lara preset
 * unmodified rather than reproducing a host application's brand palette. A host that wants its own
 * look configures it through {@link BrandingConfiguration} (logo, custom CSS URL) instead of a
 * forked preset here.
 *
 * `.cratis-dark` is the dark selector because it is the class this app puts on `<html>` and `<body>`,
 * and the one `@cratis/components/theme` keys its own dark palette off - so a single class switches
 * both. It has to be on `<html>`: Components' theme reads it from `:root` (`:root.cratis-dark`), and
 * without it the `--cratis-*` tokens follow the operating system's light/dark preference while the
 * Lara surfaces stay dark, leaving dark text on a dark card (Cratis/Ante#150). `<body>` keeps it for
 * the `color-scheme` the palette in `primereact-v10-palette.css` hangs off it.
 */
export const primeReactTheme = {
    preset: Lara,
    options: {
        darkModeSelector: '.cratis-dark',
        cssLayer: { name: 'primereact', order: 'primereact' },
    },
};
