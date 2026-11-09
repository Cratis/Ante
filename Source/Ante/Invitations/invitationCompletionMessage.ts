// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { InvitationPageContent } from '../Configuration/Configuration';

/** Resolve only successful invitation completion copy; failure and waiting messages stay under Ante's control. */
export const invitationCompletionMessage = (content: InvitationPageContent | undefined, defaultMessage: string): string =>
    content?.completionMessage || defaultMessage;
