// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AttributeTarget } from '../applyDisplayPreferencesToDocument';

/** A minimal in-memory {@link AttributeTarget} fake - avoids depending on a real DOM element (the test environment runs in Node, not jsdom). */
export class FakeAttributeTarget implements AttributeTarget {
    readonly #attributes = new Map<string, string>();

    setAttribute(name: string, value: string): void {
        this.#attributes.set(name, value);
    }

    removeAttribute(name: string): void {
        this.#attributes.delete(name);
    }

    has(name: string): boolean {
        return this.#attributes.has(name);
    }

    get(name: string): string | undefined {
        return this.#attributes.get(name);
    }
}
