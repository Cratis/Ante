// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { InvitationFlowType } from '../../Contracts/Invitations/InvitationFlowType';

/**
 * The claims this application reads out of an invitation token's payload. The token is a JWT issued
 * by the host, but Ante never verifies its signature client-side - it only peeks at the payload to
 * decide which onboarding wizard to render before the identity query resolves. The backend is the
 * one place that actually trusts the token.
 */
interface InvitationTokenPayload {
    jti?: unknown;
    invite_type?: unknown;
}

const decodeBase64Url = (value: string): string => {
    const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
    // The global `atob` (not `window.atob`) so this decoding stays usable from a plain Node test
    // environment, which has no `window` but has had a global `atob` since Node 16.
    return atob(padded);
};

const decodeInvitationTokenPayload = (token: string): InvitationTokenPayload | null => {
    const [, payload] = token.split('.');
    if (!payload) {
        return null;
    }

    try {
        return JSON.parse(decodeBase64Url(payload)) as InvitationTokenPayload;
    } catch {
        return null;
    }
};

/**
 * Reads the invitation token from the current URL - either the `/invite/{token}` path or a `token`
 * query string parameter.
 * @returns The token, or `null` when neither is present.
 */
export const getInvitationToken = (): string | null => {
    const tokenFromQuery = new URLSearchParams(window.location.search).get('token');
    const tokenFromPath = window.location.pathname.startsWith('/invite/')
        ? window.location.pathname.slice('/invite/'.length)
        : null;

    const token = tokenFromPath || tokenFromQuery;
    if (!token) {
        return null;
    }

    try {
        return decodeURIComponent(token);
    } catch {
        return token;
    }
};

/**
 * Extracts the invitation identifier from a token's `jti` claim.
 * @param token The invitation token, or `null`/`undefined` when there is none.
 * @returns The invitation identifier, or `null` when the token is absent or carries no valid `jti`.
 */
export const getInvitationIdFromToken = (token?: string | null): Guid | null => {
    if (!token) {
        return null;
    }

    const payload = decodeInvitationTokenPayload(token);
    const jti = typeof payload?.jti === 'string' ? payload.jti : '';
    return Guid.isGuid(jti) ? Guid.parse(jti) : null;
};

/**
 * Normalizes a loosely-typed value from token/identity claims into an {@link InvitationFlowType}.
 * @param value The raw claim value.
 * @returns The recognized flow type, or `null` when the value does not name one.
 */
export const normalizeFlowType = (value: unknown): InvitationFlowType | null => {
    if (value === InvitationFlowType.joinTenant || value === InvitationFlowType.createTenant) {
        return value;
    }

    if (typeof value === 'string') {
        const normalized = value.replace(/[^a-z]/gi, '').toLowerCase();

        if (normalized === 'createtenant') {
            return InvitationFlowType.createTenant;
        }

        if (normalized === 'jointenant') {
            return InvitationFlowType.joinTenant;
        }
    }

    return null;
};

/**
 * Extracts the invitation flow type from a token's `invite_type` claim.
 * @param token The invitation token, or `null`/`undefined` when there is none.
 * @returns The recognized flow type, or `null` when the token is absent or carries no valid claim.
 */
export const getFlowTypeFromInvitationToken = (token?: string | null): InvitationFlowType | null => {
    if (!token) {
        return null;
    }

    const payload = decodeInvitationTokenPayload(token);
    return normalizeFlowType(payload?.invite_type);
};
