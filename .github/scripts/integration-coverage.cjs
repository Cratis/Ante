// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

async function run({ github, context, core, env = process.env }) {
    const pr = context.payload.pull_request;
    const head = pr.head.sha;
    await core.summary
        .addHeading('Integration coverage')
        .addRaw(`PR head: ${head}\n\nMerge commit: ${context.sha}\n`)
        .write();

    if (!pr.labels.some(label => label.name === 'run-integration')) {
        core.info('Integration coverage not requested.');
        return;
    }

    const { data: current } = await github.rest.pulls.get({
        ...context.repo,
        pull_number: pr.number
    });
    if (current.head.sha !== head) {
        core.info(`Head ${head} superseded; the new head owns coverage.`);
        return;
    }
    if (!current.labels.some(label => label.name === 'run-integration')) {
        core.info('Integration coverage not requested; run-integration was removed.');
        return;
    }

    if (env.INTEGRATION === 'success' && env.BROWSER === 'success' && env.BROWSER_RAN === 'true') {
        core.info(`Integration and Browser end-to-end passed for head ${head}.`);
        return;
    }

    core.setFailed(
        `run-integration is set on PR #${pr.number} but Integration and Browser end-to-end did not both run and pass for head ${head} ` +
        `(integration: ${env.INTEGRATION}, browser: ${env.BROWSER}, browser ran: ${env.BROWSER_RAN}). ` +
        'A newer run may cover this head. Push, re-run this workflow, or remove the label and re-run this job.'
    );
}

module.exports = { run };
