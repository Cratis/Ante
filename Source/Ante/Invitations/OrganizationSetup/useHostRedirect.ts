// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useRef, useState } from 'react';
import { HostUrl } from '../../Configuration/Configuration';
import { resolveHostAppRedirectUrl } from '../../Configuration/hostAppRedirect';
import type { HostOutcomeGateDecision } from '../HostOutcomeGate';

export type HostRedirectOptions = {
    /** What the host-outcome gate currently permits. */
    gate: HostOutcomeGateDecision;
    /** The organization name the durable status already reports; empty until the host knows it. */
    statusOrganizationName: string;
    /** Message shown when setup published but the host destination could not be resolved. */
    hostAppUnavailableMessage: string;
};

/**
 * Sends the person to the host application once onboarding published and the gate permits handoff.
 * @param options The gate, the known organization name and the destination-failure message.
 * @returns The destination failures, the organization-name capture and the navigation action.
 */
export const useHostRedirect = ({ gate, statusOrganizationName, hostAppUnavailableMessage }: HostRedirectOptions) => {
    const [hostUrlResult] = HostUrl.use();
    const [errorMessages, setErrorMessages] = useState<string[]>([]);
    const organizationNameRef = useRef('');

    const navigateToHost = useCallback(() => {
        if (gate !== 'redirectAutomatically') return;
        if (!hostUrlResult.isSuccess) {
            setErrorMessages([hostAppUnavailableMessage]);
            return;
        }

        const organizationName = statusOrganizationName || organizationNameRef.current;
        if (!organizationName) return;

        try {
            const config = hostUrlResult.data;
            window.location.href = resolveHostAppRedirectUrl(config.hostAppUrl, organizationName, config.signInPath);
        } catch {
            // A malformed host configuration must present as a recoverable destination failure, not a
            // crashed page - setup already published, so the person's information was not lost.
            setErrorMessages([hostAppUnavailableMessage]);
        }
    }, [gate, hostUrlResult, statusOrganizationName, hostAppUnavailableMessage]);

    useEffect(() => {
        // Unchanged automatic redirect when the host-outcome screen was never opted into or configured -
        // 'showHostOutcome' instead lets the completion screen's own Continue action call navigateToHost,
        // and 'keepWaiting' means onboarding has not published yet, so there is nowhere to go.
        if (gate !== 'redirectAutomatically') return;
        navigateToHost();
    }, [gate, navigateToHost]);

    return {
        errorMessages,
        navigateToHost,
        captureOrganizationName: (organizationName: string) => { organizationNameRef.current = organizationName; }
    };
};
