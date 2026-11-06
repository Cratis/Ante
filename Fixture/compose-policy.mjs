// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

// Mirrors Cratis/cli Source/Cli/Commands/Run/RunStackPlanner.Ante.cs.
export const sharedWithCratisRun = [
    'ASPNETCORE_HTTP_PORTS', 'ASPNETCORE_ENVIRONMENT',
    ...['Enabled', 'Development__Enabled', 'Development__DataPath', 'Secrets__Path', 'HostStore', 'EventStore', 'Namespace', 'PublicBaseUrl', 'ApplicationUrl', 'SignUp', 'RegistrationEnabled', 'InvitationTypes__0', 'Personas__0'].map(key => `Ante__Onboarding__${key}`),
    'Cratis__Chronicle__ConnectionString', 'Cratis__MongoDB__Server', 'Cratis__MongoDB__Database',
    'Ante__Invitations__Exchange__Mode',
    ...['Issuer', 'Audience', 'LobbyScope', 'PublicKeys__0__KeyId', 'Providers__0__Key', 'Providers__0__Issuer', 'Providers__0__AcceptableAssurances__0'].map(key => `Ante__Invitations__Exchange__Attestation__${key}`)
];

export function check(config) {
    const violations = [];
    if (config.networks?.backend?.internal !== true) violations.push('backend must be internal');
    const services = config.services ?? {};
    if (!services.ante || !services.proxy) violations.push('ante and proxy services are required');
    for (const [name, service] of Object.entries(services)) {
        const networks = Object.keys(service.networks ?? {});
        if (name !== 'proxy' && (networks.length !== 1 || networks[0] !== 'backend' || service.network_mode)) violations.push(`${name} must use only backend`);
        if (service.ports?.length && name !== 'proxy') violations.push(`${name} cannot publish ports`);
        if (service.ports?.some(port => port.host_ip !== '127.0.0.1')) violations.push(`${name} ports must bind 127.0.0.1`);
        const tag = service.image?.split('/').at(-1)?.split(':')[1];
        if (!tag || tag === 'latest') violations.push(`${name} image must have an explicit non-latest tag`);
    }
    const environment = services.ante?.environment ?? {};
    for (const key of sharedWithCratisRun) {
        if (!Object.hasOwn(environment, key) || environment[key] === null || environment[key] === '') violations.push(`missing ${key}`);
    }
    for (const [key, expected] of Object.entries({
        ASPNETCORE_ENVIRONMENT: 'Development',
        Ante__Invitations__Exchange__Attestation__Issuer: 'urn:cratis:authproxy:local',
        Ante__Invitations__Exchange__Attestation__Audience: 'urn:cratis:ante:Ante:Default',
        Ante__Invitations__Exchange__Attestation__LobbyScope: 'Default'
    })) {
        if (environment[key] !== expected) violations.push(`${key} must equal ${expected}`);
    }
    return violations;
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    try {
        const config = JSON.parse(execFileSync('docker', ['compose', '-f', fileURLToPath(new URL('./compose.yml', import.meta.url)), 'config', '--format', 'json'], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }));
        const violations = check(config);
        console.log(`checked ${Object.keys(config.services ?? {}).length} services, 5 rules`);
        for (const violation of violations) console.error(violation);
        process.exitCode = violations.length ? 1 : 0;
    } catch (error) {
        console.error(`Could not check compose configuration: ${error.message}`);
        process.exitCode = 2;
    }
}
