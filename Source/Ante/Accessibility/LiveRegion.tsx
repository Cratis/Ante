// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import './LiveRegion.css';

interface LiveRegionProps {

    /** The text to announce. An empty string renders an empty, inert region. */
    message: string;

    /**
     * `'polite'` (default) waits for the screen reader to finish its current speech before
     * announcing - use for progress/step updates. `'assertive'` interrupts immediately - reserve
     * for time-sensitive errors.
     */
    politeness?: 'polite' | 'assertive';
}

/**
 * A visually-hidden ARIA live region for announcing state changes that have no visible text of
 * their own to be read - a stepper's step transition, an async operation's progress - to screen
 * reader users. Render once per page and update `message` as the announcement changes.
 */
export const LiveRegion = ({ message, politeness = 'polite' }: LiveRegionProps) => (
    <div aria-live={politeness} role={politeness === 'assertive' ? 'alert' : 'status'} className='ante-live-region'>
        {message}
    </div>
);
