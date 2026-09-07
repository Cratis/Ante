// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useReducer, useRef } from 'react';
import { OnboardingRecoveryState } from './OnboardingRecoveryState';

const DEFAULT_TIMEOUT_MS = 20000;

/**
 * React binding for {@link OnboardingRecoveryState} - keeps one instance for the component's lifetime and
 * re-renders whenever its phase may have changed, applying every durable status read as it arrives.
 * @param isRecorded Whether durable evidence confirms the operation has been recorded.
 * @param isAccepted Whether durable evidence confirms the operation has been fully published.
 * @param timeoutMs How long to wait for acceptance before moving to the `timedOut` phase.
 * @returns The live {@link OnboardingRecoveryState} instance - read `.phase`/`.isAccepted` and call
 * `.markSubmitted()` / `.checkAgain()` directly; a re-render follows automatically.
 */
export const useOnboardingRecovery = (isRecorded: boolean, isAccepted: boolean, timeoutMs = DEFAULT_TIMEOUT_MS): OnboardingRecoveryState => {
    const [, forceRender] = useReducer((renderCount: number) => renderCount + 1, 0);
    const stateRef = useRef<OnboardingRecoveryState | null>(null);
    stateRef.current ??= new OnboardingRecoveryState(forceRender, timeoutMs);

    useEffect(() => () => stateRef.current?.dispose(), []);

    useEffect(() => {
        stateRef.current?.updateStatus(isRecorded, isAccepted);
        // OnboardingRecoveryState.updateStatus is idempotent and only ever moves forward, so re-applying
        // the same durable read on every render it is unchanged is harmless.
    }, [isRecorded, isAccepted]);

    return stateRef.current;
};
