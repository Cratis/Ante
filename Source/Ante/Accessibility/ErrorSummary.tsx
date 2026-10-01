// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useRef } from 'react';
import { Message } from '@cratis/components/Display';
import { useFocusOnMount } from './useFocusOnMount';

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
 * The container is the one alert; each Message disables its own live region so a screen reader
 * announces the summary once, without nested alerts (`Cratis/Ante#151`).
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
                <Message key={index} severity='error' live={false} text={message} className={itemClassName} />
            ))}
        </div>
    );
};
