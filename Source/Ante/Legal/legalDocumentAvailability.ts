// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** A previously cached result cannot be offered for consent before a fresh query settles. */
export const legalDocumentsForDisplay = <T extends { isUnavailable: boolean }>(
    confirmed: boolean,
    status: { data: T; hasData: boolean; isSuccess: boolean; isPerforming: boolean }
): T | undefined => confirmed && status.hasData && status.isSuccess && !status.isPerforming && !status.data.isUnavailable
    ? status.data
    : undefined;
