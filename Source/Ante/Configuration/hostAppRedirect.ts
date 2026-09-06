// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Resolves the URL to redirect to on the host application once a new organization (tenant) has been
 * created, substituting the `{tenant}` placeholder `HostAppUrlConfiguration.hostAppUrl` carries with
 * the organization's name.
 * @param hostAppUrlTemplate The host application base URL template, with a `{tenant}` placeholder.
 * @param organizationName The name of the organization (tenant) that was just created.
 * @param signInPath The provider-specific sign-in path, when known.
 * @returns The URL to redirect to.
 */
export const resolveHostAppRedirectUrl = (hostAppUrlTemplate: string, organizationName: string, signInPath?: string): string => {
    const url = new URL(hostAppUrlTemplate.replace('{tenant}', organizationName), window.location.origin);

    if (!url.port && window.location.port && (url.hostname === 'localhost' || url.hostname.endsWith('.localhost'))) {
        url.port = window.location.port;
    }

    // The host commonly sits behind its own authentication proxy with its own session, so a plain
    // redirect can land on its provider-selection page - a second manual sign-in right after the
    // person just signed in here. Deep-linking the provider's sign-in path challenges it directly;
    // the identity provider still holds its session from moments ago, so the round trip completes
    // without interaction.
    if (signInPath && signInPath !== '/') {
        return new URL(signInPath, url).toString();
    }

    return url.toString();
};
