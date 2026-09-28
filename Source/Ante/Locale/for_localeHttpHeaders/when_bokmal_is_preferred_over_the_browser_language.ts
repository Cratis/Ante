// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { applyInitialLocale, applySelectedLocale } from '../applyInitialLocale';
import { localeHttpHeaders } from '../localeHttpHeaders';
import { validateOrganizationName } from '../../Organization/OrganizationNameStepValidation';
import { RegisterOrganization } from '../../Organization/Registration/Registration';
import { SetupOrganization } from '../../Invitations/OrganizationSetup/OrganizationSetup';

describe('when the browser prefers English but the visitor chose Bokmål', () => {
    const fetchRequest = vi.fn();

    beforeEach(async () => {
        vi.stubGlobal('navigator', { languages: ['en-US'], language: 'en-US' });
        vi.stubGlobal('window', { location: { search: '', protocol: 'https:' } });
        vi.stubGlobal('document', { documentElement: { setAttribute: vi.fn() }, cookie: '' });
        vi.stubGlobal('localStorage', { getItem: () => 'nb-NO', setItem: vi.fn(), removeItem: vi.fn() });
        fetchRequest.mockImplementation((url: string) => url === '/api/locale-config'
            ? Promise.resolve({ ok: true, json: async () => ({ defaultLocale: 'en', supportedLocales: ['en', 'nb-NO'] }) })
            : Promise.resolve({ status: 200, json: async () => ({
                correlationId: Guid.empty.toString(), isSuccess: true, isAuthorized: true, isValid: true,
                hasExceptions: false, validationResults: [], exceptionMessages: [], exceptionStackTrace: '',
                authorizationFailureReason: '', response: null
            }) }));
        vi.stubGlobal('fetch', fetchRequest);
        await applyInitialLocale();
    });

    afterEach(() => {
        applySelectedLocale('en');
        vi.unstubAllGlobals();
        fetchRequest.mockReset();
    });

    it('should send Bokmål on both standalone name validation requests', async () => {
        await validateOrganizationName(() => {
            const probe = new RegisterOrganization();
            probe.registrationId = Guid.empty;
            probe.setOrigin('https://ante.example');
            probe.setHttpHeadersCallback(localeHttpHeaders);
            return probe;
        }, 'ExampleAS');
        await validateOrganizationName(() => {
            const probe = new SetupOrganization();
            probe.invitationId = Guid.empty;
            probe.setOrigin('https://ante.example');
            probe.setHttpHeadersCallback(localeHttpHeaders);
            return probe;
        }, 'ExampleAS');
        fetchRequest.mock.calls.slice(1).should.have.lengthOf(2);
        fetchRequest.mock.calls.slice(1).forEach((call: unknown[]) =>
            (call[1] as RequestInit).headers!.should.have.property('Accept-Language', 'nb-NO'));
    });
});
