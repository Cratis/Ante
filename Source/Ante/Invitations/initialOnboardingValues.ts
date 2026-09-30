// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';

// Only static defaults belong here. Legal versions are supplied by LegalVersionValues after loading. Without
// legal documents there is no terms step to supply one, so the empty version a deployment without documents
// submits is seeded instead - it is a required property, and an unset one keeps the form invalid (Cratis/Ante#148).
// It is never seeded alongside documents: the form applies its initial values after the terms step has set the
// presented version, and would overwrite it.
const legalDefaults = (legalDocumentsConfigured: boolean) =>
    legalDocumentsConfigured ? { acceptedLegalTerms: false } : { acceptedLegalTerms: false, acceptedLegalVersion: '' };

export const initialRegistrationValues = (registrationId: Guid, legalDocumentsConfigured: boolean) => ({
    registrationId, organizationName: '', firstName: '', middleName: '', lastName: '', ...legalDefaults(legalDocumentsConfigured)
});

export const initialOrganizationSetupValues = (invitationId: Guid, legalDocumentsConfigured: boolean) => ({
    invitationId, organizationName: '', firstName: '', middleName: '', lastName: '', ...legalDefaults(legalDocumentsConfigured)
});

export const initialUserSetupValues = (invitationId: Guid, legalDocumentsConfigured: boolean) => ({
    invitationId, firstName: '', middleName: '', lastName: '', ...legalDefaults(legalDocumentsConfigured)
});
