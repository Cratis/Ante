// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AriaElement } from '../stepperAria';

/** A minimal in-memory {@link AriaElement} fake - avoids depending on a real DOM element (the test environment runs in Node, not jsdom). */
export class FakeAriaElement implements AriaElement {
    readonly #attributes = new Map<string, string>();

    setAttribute(name: string, value: string): void {
        this.#attributes.set(name, value);
    }

    get(name: string): string | undefined {
        return this.#attributes.get(name);
    }
}
