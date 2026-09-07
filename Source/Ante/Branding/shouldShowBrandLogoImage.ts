// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Decides whether a configured `Ante:LogoUrl` image should render, or the neutral text wordmark
 * should show instead - true only when a logo URL is configured and it has not already failed to
 * load. Pulled out of `useBrandLogo` so the decision is directly testable without a DOM/React render -
 * `Cratis/Ante#21`'s broken-image fallback.
 * @param logoUrl The configured logo URL, or `undefined`/empty when none is configured.
 * @param hasFailed Whether the configured image has already failed to load.
 */
export const shouldShowBrandLogoImage = (logoUrl: string | undefined, hasFailed: boolean): boolean =>
    Boolean(logoUrl) && !hasFailed;
