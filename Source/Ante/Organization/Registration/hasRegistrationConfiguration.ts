// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** The part of the registration configuration query result {@link hasRegistrationConfiguration} looks at. */
export interface RegistrationConfigurationResult {
    hasData: boolean;
    data?: { isEnabled?: unknown } | null;
}

/**
 * Whether the deployment's registration configuration has arrived. Arc starts the query with the proxy's
 * default value (`{}`), which `hasData` counts as data; reading its missing `isEnabled` as false would show
 * "sign-up is not available" to every visitor until the real configuration arrives (Cratis/Ante#146).
 * @param result The registration configuration query result.
 * @returns True once the configuration has arrived.
 */
export const hasRegistrationConfiguration = (result: RegistrationConfigurationResult): boolean =>
    result.hasData && typeof result.data?.isEnabled === 'boolean';
