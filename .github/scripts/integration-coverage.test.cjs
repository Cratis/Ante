// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const { run } = require('./integration-coverage.cjs');

const head = 'a'.repeat(40);
const merge = 'b'.repeat(40);
const labels = [{ name: 'run-integration' }];

function fixture({ payloadLabels = labels, currentLabels = labels, currentHead = head, apiError, env = {} } = {}) {
    const calls = { api: [], info: [], failures: [], summary: '', writes: 0 };
    const context = {
        repo: { owner: 'Cratis', repo: 'Ante' },
        sha: merge,
        payload: { pull_request: { number: 191, head: { sha: head }, labels: payloadLabels } }
    };
    const github = { rest: { pulls: { get: async args => {
        calls.api.push(args);
        if (apiError) throw apiError;
        return { data: { head: { sha: currentHead }, labels: currentLabels } };
    } } } };
    const summary = {
        addHeading(text) { calls.summary += text; return this; },
        addRaw(text) { calls.summary += text; return this; },
        async write() { calls.writes++; }
    };
    const core = {
        summary,
        info: message => calls.info.push(message),
        setFailed: message => calls.failures.push(message)
    };
    return {
        calls,
        execute: () => run({ github, context, core, env: { INTEGRATION: 'success', BROWSER: 'success', BROWSER_RAN: 'true', ...env } })
    };
}

function assertPassed(calls, message) {
    assert.deepEqual(calls.failures, []);
    assert.equal(calls.info.length, 1);
    assert.match(calls.info[0], message);
}

test('without the payload label, passes without calling the API even if results failed', async () => {
    const { calls, execute } = fixture({ payloadLabels: [], env: { INTEGRATION: 'failure' } });
    await execute();
    assertPassed(calls, /not requested/);
    assert.deepEqual(calls.api, []);
});

test('a superseded head passes even if integration failed', async () => {
    const { calls, execute } = fixture({ currentHead: 'c'.repeat(40), env: { INTEGRATION: 'failure' } });
    await execute();
    assertPassed(calls, /superseded; the new head owns coverage/);
});

test('removing the current label passes even if browser did not run', async () => {
    const { calls, execute } = fixture({ currentLabels: [], env: { BROWSER_RAN: 'false' } });
    await execute();
    assertPassed(calls, /not requested/);
});

test('both suites passing for the current labelled head passes and summarizes both SHAs', async () => {
    const { calls, execute } = fixture();
    await execute();
    assertPassed(calls, /passed for head/);
    assert.deepEqual(calls.api, [{ owner: 'Cratis', repo: 'Ante', pull_number: 191 }]);
    assert.ok(calls.summary.includes(`PR head: ${head}`));
    assert.ok(calls.summary.includes(`Merge commit: ${merge}`));
    assert.equal(calls.writes, 1);
});

for (const suite of ['INTEGRATION', 'BROWSER']) {
    for (const result of ['skipped', 'failure', 'cancelled', '']) {
        test(`${suite} ${result || 'missing'} fails closed for the current labelled head`, async () => {
            const { calls, execute } = fixture({ env: { [suite]: result } });
            await execute();
            assert.equal(calls.failures.length, 1);
            assert.ok(calls.failures[0].includes(`PR #191`));
            assert.ok(calls.failures[0].includes(head));
            assert.match(calls.failures[0], /Push, re-run this workflow, or remove the label and re-run this job/);
            assert.deepEqual(calls.info, []);
        });
    }
}

for (const ran of ['false', '', undefined]) {
    test(`browser success with ran=${ran} fails closed (including unavailable license)`, async () => {
        const { calls, execute } = fixture({ env: { BROWSER_RAN: ran } });
        await execute();
        assert.equal(calls.failures.length, 1);
        assert.match(calls.failures[0], /did not both run and pass/);
        assert.ok(calls.failures[0].includes(head));
    });
}

test('an API error rejects rather than passing', async () => {
    const apiError = new Error('API unavailable');
    const { calls, execute } = fixture({ apiError });
    await assert.rejects(execute, error => error === apiError);
    assert.equal(calls.api.length, 1);
    assert.deepEqual(calls.info, []);
});

const workflow = readFileSync(path.join(__dirname, '../workflows/integration.yml'), 'utf8');
const jobs = Object.fromEntries([...workflow.split('\njobs:\n')[1].matchAll(/^  (\w+):\n([\s\S]*?)(?=^  \w+:|$(?![\s\S]))/gm)]
    .map(match => [match[1], match[2]]));
const suffix = "${{ github.event.action == 'labeled' && github.event.label.name != 'run-integration' && format(' - {0}', github.event.label.name) || '' }}";
const suiteCondition = "github.event_name != 'pull_request' || (contains(github.event.pull_request.labels.*.name, 'run-integration') && (github.event.action != 'labeled' || github.event.label.name == 'run-integration'))";

test('workflow reruns on synchronize and reopened, and quotes the run name containing the head SHA', () => {
    assert.match(workflow, /pull_request:\n    types: \[labeled, synchronize, reopened\]/);
    const runName = workflow.match(/^run-name: (.+)$/m)[1];
    assert.ok(runName.startsWith('"') && runName.endsWith('"'));
    assert.ok(runName.includes("github.event_name == 'pull_request'"));
    assert.ok(runName.includes("format(' - PR #{0} @ {1}', github.event.pull_request.number, github.event.pull_request.head.sha)"));
});

test('all three job names suffix unrelated labels without changing suite conditions', () => {
    assert.deepEqual(Object.keys(jobs), ['integration', 'browser', 'coverage']);
    for (const [id, name] of [['integration', 'Integration'], ['browser', 'Browser end-to-end'], ['coverage', 'Integration coverage']]) {
        assert.equal(jobs[id].match(/^    name: (.+)$/m)[1], name + suffix);
    }
    for (const id of ['integration', 'browser']) {
        const condition = jobs[id].match(/    if: >-\n([\s\S]*?)(?=^    \S)/m)[1].trim().replace(/\s+/g, ' ');
        assert.equal(condition, suiteCondition);
        assert.match(jobs[id], /cancel-in-progress: \$\{\{ github.event_name == 'pull_request' \}\}/);
    }
});

test('coverage runs after both suites even when skipped or failed, excluding unrelated labels', () => {
    assert.match(jobs.coverage, /needs: \[integration, browser\]/);
    assert.ok(jobs.coverage.includes("if: ${{ !cancelled() && github.event_name == 'pull_request' && (github.event.action != 'labeled' || github.event.label.name == 'run-integration') }}"));
    assert.match(jobs.coverage, /permissions:\n      contents: read\n      pull-requests: read/);
    assert.match(jobs.coverage, /timeout-minutes: 5/);
});

test('coverage checks out its tests and workflow, tests the policy, and passes both results and browser ran', () => {
    assert.match(jobs.browser, /outputs:\n      ran: \$\{\{ steps.license.outputs.available \}\}/);
    assert.match(jobs.browser, /id: license/);
    assert.match(jobs.coverage, /uses: actions\/checkout@v4/);
    assert.match(jobs.coverage, /sparse-checkout: \|\n            \.github\/scripts\n            \.github\/workflows\/integration.yml\n          sparse-checkout-cone-mode: false/);
    assert.match(jobs.coverage, /run: node --test \.github\/scripts\/integration-coverage.test.cjs/);
    assert.match(jobs.coverage, /uses: actions\/github-script@v7/);
    for (const [name, value] of [['INTEGRATION', 'needs.integration.result'], ['BROWSER', 'needs.browser.result'], ['BROWSER_RAN', 'needs.browser.outputs.ran']]) {
        assert.ok(jobs.coverage.includes(`${name}: \${{ ${value} }}`));
    }
    assert.ok(jobs.coverage.includes("await require('./.github/scripts/integration-coverage.cjs').run({github, context, core});"));
});
