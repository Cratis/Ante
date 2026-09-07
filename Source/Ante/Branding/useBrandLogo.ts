// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { shouldShowBrandLogoImage } from './shouldShowBrandLogoImage';

/** The reactive surface a branded heading (`OrganizationSetupFrame`/`UserSetupFrame`) needs to render a logo with a broken-image fallback. */
export interface BrandLogoState {
    /** Whether the logo image should be rendered. `false` when no logo is configured, or the configured image failed to load - in both cases the neutral text wordmark renders instead. */
    readonly showImage: boolean;

    /** Attach to the `<img>`'s `onError` - flips {@link showImage} to `false` for the remainder of this configured URL. */
    readonly onImageError: () => void;
}

/**
 * Tracks whether a configured `Ante:LogoUrl` image is safe to render, giving it a broken-image
 * fallback (`Cratis/Ante#21`): a 404, a revoked URL, or any other image load failure falls back to the
 * neutral text wordmark instead of the browser's broken-image icon.
 * @param logoUrl The configured logo URL, or `undefined`/empty when none is configured.
 */
export const useBrandLogo = (logoUrl: string | undefined): BrandLogoState => {
    const [hasFailed, setHasFailed] = useState(false);

    // A different (or newly-configured) URL deserves its own chance to load, rather than staying
    // permanently in the failed state of whatever URL was configured before it.
    useEffect(() => setHasFailed(false), [logoUrl]);

    return {
        showImage: shouldShowBrandLogoImage(logoUrl, hasFailed),
        onImageError: () => setHasFailed(true)
    };
};
