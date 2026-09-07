// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The minimal surface {@link applyDisplayAttributes} needs from a target element. Narrowed down from
 * `Element` (rather than typed as `HTMLElement` directly) so specs can pass a plain fake backed by a
 * `Map` instead of requiring a real DOM.
 */
export interface AttributeTarget {
    setAttribute(name: string, value: string): void;
    removeAttribute(name: string): void;
}

/**
 * Applies a map of `data-*` attributes (as produced by `toDisplayAttributes`) to a target element -
 * normally `document.documentElement`. Setting is idempotent and order-independent: a `null` value
 * removes the attribute, anything else sets it, so calling this repeatedly as preferences change
 * always converges on exactly the attributes the current preferences describe, never accumulating
 * stale ones from an earlier state.
 * @param target The element to write attributes to.
 * @param attributes The attribute map to apply.
 */
export const applyDisplayAttributes = (target: AttributeTarget, attributes: Record<string, string | null>): void => {
    for (const [name, value] of Object.entries(attributes)) {
        if (value === null) {
            target.removeAttribute(name);
        } else {
            target.setAttribute(name, value);
        }
    }
};
