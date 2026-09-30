// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BrowserContext, expect } from '@playwright/test';

/** The E2E host's control endpoint and host application; the lobbies listen on the next two ports. */
export const controlPort = Number(process.env.ANTE_E2E_PORT ?? 5610);
export const controlUrl = `http://localhost:${controlPort}`;

/** `plain` has no legal documents configured; `legal` has a host-provided set, so every wizard gets a terms step. */
export type LobbyName = 'plain' | 'legal';

export const lobbyUrl = (lobby: LobbyName): string => `http://localhost:${controlPort + (lobby === 'plain' ? 1 : 2)}`;

/** The cookie the E2E host turns into the authentication proxy's forwarded identity headers. */
const signInCookie = 'ante-e2e-subject';

export interface Invitation {
    invitationId: string;
    token: string;
    subject: string;
    email: string;
    link: string;
}

/**
 * Has the lobby's host publish an invitation and the authentication proxy exchange its token for a new invitee.
 * @param lobby The lobby to invite into.
 * @param kind `join` to join an organization, `create` to set one up.
 * @returns The invitation, with the link the invitee opens.
 */
export const invite = async (lobby: LobbyName, kind: 'join' | 'create'): Promise<Invitation> => {
    const response = await fetch(`${controlUrl}/lobbies/${lobby}/invitations/${kind}`, { method: 'POST' });
    expect(response.ok, `publishing a ${kind} invitation in the ${lobby} lobby`).toBe(true);
    return await response.json() as Invitation;
};

/**
 * Gets the names of the events the lobby's host has received from Ante for an invitation or registration.
 * @param lobby The lobby.
 * @param eventSourceId The invitation or registration.
 * @returns The event type names, in delivery order.
 */
export const received = async (lobby: LobbyName, eventSourceId: string): Promise<string[]> => {
    const response = await fetch(`${controlUrl}/lobbies/${lobby}/received/${eventSourceId}`);
    expect(response.ok, `reading what the ${lobby} host received`).toBe(true);
    return await response.json() as string[];
};

/**
 * Signs every page of a browser context in as a subject, the way the authentication proxy's session would.
 * @param context The browser context.
 * @param subject The subject.
 */
export const signIn = async (context: BrowserContext, subject: string): Promise<void> => {
    await context.addCookies([{ name: signInCookie, value: subject, url: lobbyUrl('plain') }]);
};

/** A subject nobody has used before. */
export const newSubject = (prefix: string): string => `${prefix}-${crypto.randomUUID().replaceAll('-', '')}`;

/** An organization name nobody has used before, valid as a host namespace. */
export const newOrganizationName = (): string => `E2E${crypto.randomUUID().replaceAll('-', '').slice(0, 12)}`;
