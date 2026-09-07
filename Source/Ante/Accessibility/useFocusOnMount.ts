// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RefObject, useEffect } from 'react';

/**
 * Moves focus to the referenced element as soon as it mounts - used for error summaries so a
 * screen-reader user lands directly on the newly-appeared error rather than having to discover it by
 * continuing to tab through the page. The element must be focusable (e.g. `tabIndex={-1}`).
 * @param ref The element to focus once mounted.
 */
export const useFocusOnMount = (ref: RefObject<HTMLElement | null>): void => {
    // Deliberately mount-only: this announces a summary appearing, not every re-render while it stays visible.
    useEffect(() => {
        ref.current?.focus();
    }, [ref]);
};
