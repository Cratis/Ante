// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useState } from 'react';
import { HostUrl } from '../../Configuration/Configuration';
import type { HostOutcomeGateDecision } from '../HostOutcomeGate';
import strings from 'Strings';

/**
 * Sends the person to the host application once acceptance published and the gate permits handoff.
 * @param hostOutcomeGate What the host-outcome gate currently permits.
 * @returns The destination failures and the navigation action.
 */
export const useUserSetupHostRedirect = (hostOutcomeGate: HostOutcomeGateDecision) => {
    const [hostUrlResult] = HostUrl.use();
    const [errorMessages, setErrorMessages] = useState<string[]>([]);

    const navigateToHost = useCallback(() => {
        if (hostOutcomeGate !== 'redirectAutomatically') return;
        if (!hostUrlResult.isSuccess) {
            setErrorMessages([strings.userSetup.hostAppUrlUnavailable]);
            return;
        }

        try {
            // The provider-specific sign-in path challenges the provider the person just used, so
            // entering the host application is a silent round trip instead of a second provider
            // selection.
            const config = hostUrlResult.data;
            window.location.href = config.signInPath && config.signInPath !== '/'
                ? new URL(config.signInPath, new URL(config.hostAppUrl, window.location.origin)).toString()
                : config.hostAppUrl;
        } catch {
            // A malformed host configuration must present as a recoverable destination failure, not a
            // crashed page - acceptance already published, so the person's information was not lost.
            setErrorMessages([strings.userSetup.hostAppUrlUnavailable]);
        }
    }, [hostOutcomeGate, hostUrlResult]);

    useEffect(() => {
        // Unchanged automatic redirect when the optional host-outcome screen was never configured -
        // 'showHostOutcome' instead lets the completion screen's own Continue action call
        // navigateToHost, and 'keepWaiting' means acceptance has not published yet.
        if (hostOutcomeGate !== 'redirectAutomatically') return;
        navigateToHost();
    }, [hostOutcomeGate, navigateToHost]);

    return { errorMessages, navigateToHost };
};
