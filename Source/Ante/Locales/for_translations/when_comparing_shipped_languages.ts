// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import english from '../en/translation.json';
import bokmal from '../nb/translation.json';

const paths = (value: Record<string, unknown>, prefix = ''): string[] => Object.entries(value).flatMap(([key, child]) => {
    const path = prefix ? `${prefix}.${key}` : key;
    return child !== null && typeof child === 'object' ? paths(child as Record<string, unknown>, path) : [path];
}).sort();

describe('when comparing shipped translation files', () => {
    it('should have exactly the same keys', () => paths(bokmal).should.deep.equal(paths(english)));
    it('should retain the same interpolation placeholders', () => {
        const flatten = (value: Record<string, unknown>): string[] => Object.values(value).flatMap(child =>
            child !== null && typeof child === 'object'
                ? flatten(child as Record<string, unknown>)
                : [...String(child).matchAll(/\{[^}]+\}/g)].map(match => match[0]));
        flatten(bokmal).should.deep.equal(flatten(english));
    });
});
