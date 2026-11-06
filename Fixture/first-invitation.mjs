// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { randomUUID } from 'node:crypto';
import { parseArgs } from 'node:util';
import { setTimeout as delay } from 'node:timers/promises';

let base;
let deadline;
let invitationId;
let passed = 0;
let currentStep = 'arguments';

function expect(condition, message) {
    if (!condition) throw new Error(message);
}

async function request(path, options = {}) {
    const url = new URL(path, base);
    expect(url.origin === base.origin, 'Fixture URL must stay on the configured proxy origin');
    return fetch(url, { ...options, redirect: 'manual', signal: AbortSignal.timeout(Math.max(1, Math.min(10000, deadline - Date.now()))) });
}

async function poll(operation) {
    let detail = 'condition not reached';
    while (Date.now() < deadline) {
        try {
            const result = await operation();
            if (result) return result;
        } catch (error) {
            detail = error.message;
        }
        await delay(Math.min(500, Math.max(0, deadline - Date.now())));
    }
    throw new Error(`Timed out: ${detail}`);
}

async function step(name, operation) {
    currentStep = name;
    const result = await operation();
    passed++;
    console.log(`✓ ${name}`);
    return result;
}

async function hostStatus() {
    const response = await request(`/host/invitations/${invitationId}`);
    expect(response.ok, `Host status returned HTTP ${response.status}`);
    return response.json();
}

try {
    const { values } = parseArgs({ options: {
        'base-url': { type: 'string', default: `http://localhost:${process.env.ANTE_FIXTURE_PORT ?? 8080}` },
        timeout: { type: 'string', default: '300' }
    } });
    base = new URL(values['base-url']);
    const seconds = Number(values.timeout);
    expect(['http:', 'https:'].includes(base.protocol), 'base-url must be HTTP or HTTPS');
    expect(Number.isFinite(seconds) && seconds > 0 && seconds <= 3600, 'timeout must be between 0 and 3600 seconds');
    deadline = Date.now() + seconds * 1000;

    await step('fixture ready', () => poll(async () => (await request('/fixture/ready')).status === 200));
    const email = `fixture-${randomUUID()}@example.com`;
    invitationId = await step('host invited member', async () => {
        const response = await request('/host/invitations', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, tenant: 'Acme', roles: ['Member'] })
        });
        expect(response.ok, `Invite returned HTTP ${response.status}`);
        const result = await response.json();
        expect(typeof result.invitationId === 'string', 'Host did not return an invitation id');
        return result.invitationId;
    });
    const issued = await step('Ante issued capability', () => poll(async () => {
        const status = await hostStatus();
        return status.tokenIssued && status.link ? status : false;
    }));
    const state = await step('proxy validated and staged invitation', async () => {
        const response = await request(issued.link);
        expect(response.status === 302, `Stage returned HTTP ${response.status}, expected 302`);
        const location = new URL(response.headers.get('location'), base);
        expect(location.origin === base.origin && location.pathname === '/fixture/sign-in', 'Stage did not redirect to fixture sign-in');
        expect(location.searchParams.has('state'), 'Stage did not return sign-in state');
        return location.searchParams.get('state');
    });
    const cookie = await step('stub sign-in completed exchange', async () => {
        const response = await request('/fixture/sign-in', {
            method: 'POST', body: new URLSearchParams({ state, email })
        });
        expect(response.status === 302, `Sign-in returned HTTP ${response.status}, expected 302`);
        const session = response.headers.getSetCookie().find(value => value.startsWith('ante-fixture-session='))?.split(';')[0];
        expect(session, 'Sign-in did not return the fixture session cookie');
        return session;
    });
    await step('user setup succeeded', () => poll(async () => {
        const response = await request('/api/invitations/user-setup', {
            method: 'POST', headers: { 'Content-Type': 'application/json', Cookie: cookie },
            body: JSON.stringify({ invitationId, firstName: 'Jane', middleName: '', lastName: 'Doe', acceptedLegalTerms: false, acceptedLegalVersion: '' })
        });
        expect(response.ok, `User setup returned HTTP ${response.status}`);
        return (await response.json()).isSuccess === true;
    }));
    await step('host received acceptance', () => poll(async () => (await hostStatus()).accepted));
    await step('Ante confirmed host provisioning', () => poll(async () => (await hostStatus()).provisioningConfirmed));
    console.log(`first invitation: ${passed}/8 steps passed`);
} catch (error) {
    console.error(`✗ ${currentStep}: ${error.message}`);
    if (invitationId) {
        try {
            // Diagnostics get a bounded fresh request even when the journey's deadline expired.
            deadline = Date.now() + 5000;
            console.error(`host received: ${JSON.stringify((await hostStatus()).received)}`);
        } catch (diagnosticError) {
            console.error(`host received: unavailable (${diagnosticError.message})`);
        }
    }
    console.error(`first invitation: ${passed}/8 steps passed`);
    process.exitCode = passed === 0 ? 2 : 1;
}
