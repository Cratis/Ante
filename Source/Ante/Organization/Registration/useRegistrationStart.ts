// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState, type RefObject } from 'react';
import { Guid } from '@cratis/fundamentals';
import { startRegistration } from './startRegistration';

type RegistrationStartOptions = {
    /** The durable registration identifier. */
    registrationId: Guid;
    /** Whether the durable status was read and the hand-off is in its form phase. */
    canStart: boolean;
    /** Marks the hand-off submitted when the server already has this registration. */
    markSubmittedRef: RefObject<() => void>;
};

/**
 * Starts the durable registration once the form phase is reached, and lets the person retry a failed start.
 * @param options The registration to start and the hand-off state that gates it.
 * @returns The start state and the retry action.
 */
export const useRegistrationStart = ({ registrationId, canStart, markSubmittedRef }: RegistrationStartOptions) => {
    const [startState, setStartState] = useState<'pending' | 'started' | 'failed'>('pending');
    const [startAttempt, setStartAttempt] = useState(0);

    useEffect(() => {
        if (!canStart) return;
        let active = true;
        void startRegistration(registrationId).then(outcome => {
            if (!active) return;
            if (outcome === 'resume') markSubmittedRef.current();
            else setStartState(outcome);
        });
        return () => { active = false; };
    }, [registrationId, canStart, startAttempt, markSubmittedRef]);

    return { startState, retry: () => { setStartState('pending'); setStartAttempt(attempt => attempt + 1); } };
};
