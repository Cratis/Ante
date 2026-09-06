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
 * `.cratis-dark` is the dark selector because it is the class this app puts on `<body>`, and the one
 * `@cratis/components/theme` keys its own dark palette off - so a single class switches both.
 */
export const primeReactTheme = {
    preset: Lara,
    options: {
        darkModeSelector: '.cratis-dark',
        cssLayer: { name: 'primereact', order: 'primereact' },
    },
};
