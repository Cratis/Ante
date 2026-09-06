// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The documents making up the legal document set a user accepts.
 *
 * The values double as the placeholder names used in the acceptance label template, so a translation can put
 * the two links wherever the sentence needs them.
 */
export enum LegalDocumentKind {
    TermsAndConditions = 'termsAndConditions',
    PrivacyPolicy = 'privacyPolicy'
}
