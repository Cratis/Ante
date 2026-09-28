// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';

// Only static defaults belong here. Legal versions are supplied by LegalVersionValues after loading.
export const initialRegistrationValues = (registrationId: Guid) => ({
    registrationId, organizationName: '', firstName: '', middleName: '', lastName: '', acceptedLegalTerms: false
});

export const initialOrganizationSetupValues = (invitationId: Guid) => ({
    invitationId, organizationName: '', firstName: '', middleName: '', lastName: '', acceptedLegalTerms: false
});

export const initialUserSetupValues = (invitationId: Guid) => ({
    invitationId, firstName: '', middleName: '', lastName: '', acceptedLegalTerms: false
});
