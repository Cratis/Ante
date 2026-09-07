// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Scheduler } from '../OnboardingRecoveryState';

/**
 * A controllable {@link Scheduler} fake shared by {@link OnboardingRecoveryState} specs, so a timeout can
 * be fired deterministically instead of depending on real timers.
 */
export class FakeScheduler implements Scheduler {
    #scheduled: { callback: () => void; delayMs: number } | null = null;

    get isScheduled(): boolean {
        return this.#scheduled !== null;
    }

    get scheduledDelayMs(): number | undefined {
        return this.#scheduled?.delayMs;
    }

    schedule(callback: () => void, delayMs: number): () => void {
        this.#scheduled = { callback, delayMs };
        return () => {
            if (this.#scheduled?.callback === callback) this.#scheduled = null;
        };
    }

    fire(): void {
        const scheduled = this.#scheduled;
        this.#scheduled = null;
        scheduled?.callback();
    }
}
