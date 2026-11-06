// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { check, sharedWithCratisRun } from './compose-policy.mjs';

function valid() {
    return {
        networks: { backend: { internal: true }, edge: {} },
        services: {
            ante: {
                image: 'ante-fixture/ante:local', networks: { backend: null },
                environment: {
                    ...Object.fromEntries(sharedWithCratisRun.map(key => [key, 'fixture'])),
                    ASPNETCORE_ENVIRONMENT: 'Development',
                    Ante__Invitations__Exchange__Attestation__Issuer: 'urn:cratis:authproxy:local',
                    Ante__Invitations__Exchange__Attestation__Audience: 'urn:cratis:ante:Ante:Default',
                    Ante__Invitations__Exchange__Attestation__LobbyScope: 'Default'
                }
            },
            proxy: { image: 'ante-fixture/proxy:local', networks: { backend: null, edge: null }, ports: [{ host_ip: '127.0.0.1', published: '8080', target: 8080 }] }
        }
    };
}

test('the isolated, tagged fixture with the cratis run keys passes', () => assert.deepEqual(check(valid()), []));

for (const [name, mutate] of [
    ['backend must be internal', config => { config.networks.backend.internal = false; }],
    ['only proxy may use edge', config => { config.services.ante.networks.edge = null; }],
    ['backends cannot publish ports', config => { config.services.ante.ports = [{ host_ip: '127.0.0.1' }]; }],
    ['published ports must be loopback', config => { config.services.proxy.ports[0].host_ip = '0.0.0.0'; }],
    ['images must have tags', config => { config.services.ante.image = 'ante-fixture/ante'; }],
    ['latest is forbidden', config => { config.services.ante.image = 'ante-fixture/ante:latest'; }],
    ['development environment is required', config => { config.services.ante.environment.ASPNETCORE_ENVIRONMENT = 'Production'; }],
    ['issuer is pinned', config => { config.services.ante.environment.Ante__Invitations__Exchange__Attestation__Issuer = 'wrong'; }],
    ['audience is pinned', config => { config.services.ante.environment.Ante__Invitations__Exchange__Attestation__Audience = 'wrong'; }],
    ['scope is pinned', config => { config.services.ante.environment.Ante__Invitations__Exchange__Attestation__LobbyScope = 'wrong'; }]
]) {
    test(name, () => {
        const config = valid();
        mutate(config);
        assert.ok(check(config).length > 0);
    });
}

for (const key of sharedWithCratisRun) {
    test(`requires ${key}`, () => {
        const config = valid();
        delete config.services.ante.environment[key];
        assert.ok(check(config).some(violation => violation.includes(key)));
    });
}
