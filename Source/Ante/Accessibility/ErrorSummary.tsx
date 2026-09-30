// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useRef } from 'react';
import { useFocusOnMount } from './useFocusOnMount';
import './ErrorSummary.css';

interface ErrorSummaryProps {

    /** The error messages to list. */
    messages: string[];

    /** Class applied to the summary container, alongside the page's own layout class. */
    className?: string;

    /** Class applied to each individual message. */
    itemClassName?: string;
}

/**
 * An error summary that announces itself and receives focus the moment it appears, so a
 * screen-reader user lands directly on it instead of discovering it later by continuing to tab
 * through the page - `Cratis/Ante#20`'s "error-summary focus".
 *
 * The container is the one alert; the messages inside it are plain paragraphs. Cratis Components'
 * `Message` is a `role='alert'` of its own for errors, so wrapping it here put every message in two
 * nested live regions and a screen reader read it twice (`Cratis/Ante#151`). The messages borrow the
 * Components surface tokens instead, so they still look like its messages.
 *
 * Onboarding pages mount a fresh instance only when errors appear, whether in the page's failure
 * state or above an active form, so focus moves to the newly reported message.
 */
export const ErrorSummary = ({ messages, className, itemClassName }: ErrorSummaryProps) => {
    const ref = useRef<HTMLDivElement>(null);
    useFocusOnMount(ref);

    return (
        <div ref={ref} role='alert' tabIndex={-1} className={className}>
            {messages.map((message, index) => (
                <p key={index} className={['ante-error-summary__item', itemClassName].filter(Boolean).join(' ')}>
                    <span className='ante-error-summary__icon' aria-hidden='true'>{'\u2A2F'}</span>
                    <span>{message}</span>
                </p>
            ))}
        </div>
    );
};
