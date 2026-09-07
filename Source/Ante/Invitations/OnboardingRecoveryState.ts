// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OnboardingRecoveryPhase } from './OnboardingRecoveryPhase';

/**
 * Injectable source of the current time and injectable scheduler, so {@link OnboardingRecoveryState}'s
 * timeout behavior is testable without the real clock or real timers. Defined alongside the class rather
 * than in their own files - like a component's own props type - because they exist solely to parameterize
 * this one class's constructor and have no meaning on their own.
 */
export type Clock = { now(): number };
export type Scheduler = { schedule(callback: () => void, delayMs: number): () => void };

export const systemClock: Clock = { now: () => Date.now() };

export const systemScheduler: Scheduler = {
    schedule: (callback, delayMs) => {
        const handle = globalThis.setTimeout(callback, delayMs);
        return () => globalThis.clearTimeout(handle);
    }
};

/**
 * Derives which phase an onboarding wizard should render from durable status rather than from a
 * process-local flag, so a reload, a second tab, or returning later after a drop resolves to the right
 * place instead of a dead end or a duplicate submission. Recorded-but-not-yet-accepted and "I just
 * submitted, the first status read has not arrived yet" are treated identically - both mean keep waiting,
 * never show the form again once something has been submitted.
 *
 * Timing out never claims failure - it only stops waiting locally while status keeps being observed, so a
 * late acceptance still resolves the flow: {@link updateStatus} always clears a local timeout the moment
 * durable evidence confirms acceptance.
 */
export class OnboardingRecoveryState {
    #submitted = false;
    #recorded = false;
    #accepted = false;
    #timedOut = false;
    #cancelTimer: (() => void) | null = null;

    /**
     * Initializes a new instance of the {@link OnboardingRecoveryState} class.
     * @param onChange Invoked whenever the phase may have changed, so the caller can re-render.
     * @param timeoutMs How long to wait for acceptance before moving to the `timedOut` phase.
     * @param clock The clock to read the current time from.
     * @param scheduler The scheduler used to arm the timeout.
     */
    constructor(
        readonly onChange: () => void,
        readonly timeoutMs: number,
        readonly clock: Clock = systemClock,
        readonly scheduler: Scheduler = systemScheduler) {
    }

    /** Gets whether the operation has been submitted, recorded, or accepted - i.e. no longer safe to (re)submit. */
    get isWaiting(): boolean {
        return this.#submitted || this.#recorded || this.#accepted;
    }

    /** Gets whether durable evidence has confirmed full publication - the only state safe to hand off to the host. */
    get isAccepted(): boolean {
        return this.#accepted;
    }

    /** Gets the phase the wizard should currently render. */
    get phase(): OnboardingRecoveryPhase {
        if (!this.isWaiting) return 'form';
        return this.#timedOut ? 'timedOut' : 'waiting';
    }

    /**
     * Applies the latest durable status read. Safe to call repeatedly with the same values - a
     * reconnect's read is idempotent - and never regresses an already-observed submission.
     * @param recorded Whether durable evidence confirms the operation has been recorded.
     * @param accepted Whether durable evidence confirms the operation has been fully published.
     */
    updateStatus(recorded: boolean, accepted: boolean): void {
        const wasWaiting = this.isWaiting;
        this.#recorded = this.#recorded || recorded;
        this.#accepted = this.#accepted || accepted;

        if (this.#accepted) {
            // Late success always wins over a local timeout - stop waiting locally and never present a
            // just-confirmed acceptance as unconfirmed.
            this.#timedOut = false;
            this.disarmTimer();
        } else if (!wasWaiting && this.isWaiting) {
            this.armTimer();
        }

        this.onChange();
    }

    /** Call once the wizard's own command has succeeded, to switch to the waiting phase immediately. */
    markSubmitted(): void {
        const wasWaiting = this.isWaiting;
        this.#submitted = true;
        if (!wasWaiting) this.armTimer();
        this.onChange();
    }

    /**
     * Resets the timeout window. Does not create a new operation and does not stop observing durable
     * status - a late acceptance is handled independently of this call, via {@link updateStatus}.
     */
    checkAgain(): void {
        if (this.#accepted) return;
        this.#timedOut = false;
        this.armTimer();
        this.onChange();
    }

    /** Releases the pending timeout, if any. Call from a component's unmount cleanup. */
    dispose(): void {
        this.disarmTimer();
    }

    private armTimer(): void {
        this.disarmTimer();
        this.#cancelTimer = this.scheduler.schedule(() => {
            this.#cancelTimer = null;
            this.#timedOut = true;
            this.onChange();
        }, this.timeoutMs);
    }

    private disarmTimer(): void {
        this.#cancelTimer?.();
        this.#cancelTimer = null;
    }
}
