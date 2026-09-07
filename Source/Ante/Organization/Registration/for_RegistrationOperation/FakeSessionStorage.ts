// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * A minimal in-memory `Storage` fake for {@link RegistrationOperation} specs - the test environment runs
 * in Node, which has no `sessionStorage` global, so a fake is stubbed in via `vi.stubGlobal`.
 */
export class FakeSessionStorage implements Storage {
    readonly #entries = new Map<string, string>();

    get length(): number {
        return this.#entries.size;
    }

    getItem(key: string): string | null {
        return this.#entries.get(key) ?? null;
    }

    setItem(key: string, value: string): void {
        this.#entries.set(key, value);
    }

    removeItem(key: string): void {
        this.#entries.delete(key);
    }

    clear(): void {
        this.#entries.clear();
    }

    key(index: number): string | null {
        return Array.from(this.#entries.keys())[index] ?? null;
    }
}
