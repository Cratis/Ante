// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Verifies the structure of this folder without any dependency:
//   - every toc.yml entry resolves to a page in this folder, and every page is in the toc
//   - every relative link, image and reference resolves to a file that exists
//   - every #anchor resolves to a heading of the page it addresses
//
// External http(s) links are not checked here; verify-markdown.sh checks them
// separately and non-blocking, because a third-party outage must not fail a pull request.

import { readdir, readFile, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const documentationRoot = path.dirname(fileURLToPath(import.meta.url));
const errors = [];
const headingCache = new Map();

async function pagesBelow(directory) {
    const files = [];
    for (const entry of await readdir(directory, { withFileTypes: true })) {
        const entryPath = path.join(directory, entry.name);
        if (entry.isDirectory()) {
            files.push(...await pagesBelow(entryPath));
        } else if (/\.mdx?$/i.test(entry.name)) {
            files.push(entryPath);
        }
    }

    return files;
}

function relative(file) {
    return path.relative(path.dirname(documentationRoot), file).split(path.sep).join('/');
}

async function exists(target) {
    try {
        return await stat(target);
    } catch {
        return undefined;
    }
}

// Removes fenced code blocks and inline code spans, so that example syntax is never
// mistaken for a link or a heading. Line numbers are preserved. Headings keep their
// inline code, because it is part of the text the slug is made from.
function withoutCode(content, keepInlineCode = false) {
    let fence;
    return content.split('\n').map(line => {
        const fenceMatch = line.match(/^\s*(`{3,}|~{3,})/);
        if (fenceMatch) {
            const marker = fenceMatch[1];
            if (!fence) {
                fence = { character: marker[0], length: marker.length };
            } else if (marker[0] === fence.character && marker.length >= fence.length) {
                fence = undefined;
            }

            return '';
        }

        if (fence) return '';

        return keepInlineCode ? line : line.replace(/(`+)[^`]*?\1/g, match => ' '.repeat(match.length));
    }).join('\n');
}

// The slug the site generates for a heading (GitHub-style): lowercase, punctuation
// dropped, spaces to hyphens, and -1, -2 appended to repeats.
function headingSlugs(content) {
    const slugs = new Set();
    const counts = new Map();
    for (const line of withoutCode(stripFrontmatter(content), true).split('\n')) {
        const match = line.match(/^ {0,3}#{1,6}\s+(.*?)\s*#*\s*$/);
        if (!match) continue;

        const text = match[1]
            .replace(/!?\[([^\]]*)\]\([^)]*\)/g, '$1')
            .replace(/<[^>]+>/g, '')
            .replace(/[*`]/g, '');
        let slug = text.toLowerCase().replace(/[^\p{L}\p{N}\p{M} _-]/gu, '').replace(/ /g, '-');
        const seen = counts.get(slug) ?? 0;
        counts.set(slug, seen + 1);
        if (seen > 0) slug = `${slug}-${seen}`;
        slugs.add(slug);
    }

    return slugs;
}

function stripFrontmatter(content) {
    const match = content.match(/^---\r?\n[\s\S]*?\r?\n---\r?\n/);
    return match ? '\n'.repeat(match[0].split('\n').length - 1) + content.slice(match[0].length) : content;
}

async function headingsOf(file) {
    if (!headingCache.has(file)) {
        headingCache.set(file, headingSlugs(await readFile(file, 'utf8')));
    }

    return headingCache.get(file);
}

async function validateTarget(source, line, rawTarget, hostDirectory = path.dirname(source)) {
    let target = rawTarget.trim();
    if (target.startsWith('<') && target.includes('>')) target = target.slice(1, target.indexOf('>'));
    else target = target.split(/\s+/)[0];

    // External and site-absolute targets are not files of this folder.
    if (target === '' || /^[a-z][a-z0-9+.-]*:/i.test(target) || target.startsWith('/') || target.startsWith('//')) return;

    const [pathPart, ...fragmentParts] = target.split('#');
    const fragment = fragmentParts.join('#');
    const where = `${relative(source)}:${line}`;

    let resolved = source;
    if (pathPart !== '') {
        resolved = path.resolve(hostDirectory, decodeURI(pathPart.split('?')[0]));
        const info = await exists(resolved);
        if (!info) {
            errors.push(`${where}: '${rawTarget.trim()}' does not resolve to a file.`);
            return;
        }

        if (info.isDirectory()) {
            const index = ['index.md', 'index.mdx'].map(name => path.join(resolved, name));
            const found = (await Promise.all(index.map(exists))).findIndex(Boolean);
            if (found < 0) {
                errors.push(`${where}: '${rawTarget.trim()}' is a folder without an index page.`);
                return;
            }

            resolved = index[found];
        }
    }

    if (fragment !== '' && /\.mdx?$/i.test(resolved)) {
        const anchors = await headingsOf(resolved);
        if (!anchors.has(decodeURIComponent(fragment).toLowerCase())) {
            errors.push(`${where}: '${rawTarget.trim()}' - no heading in ${relative(resolved)} produces the anchor '#${fragment}'.`);
        }
    }
}

async function validateLinks(file) {
    const content = withoutCode(await readFile(file, 'utf8'));
    const lines = content.split('\n');
    const inline = /!?\[(?:[^\]\\]|\\.)*\]\(\s*(<[^>]*>|(?:[^()\s]|\([^()]*\))*)(?:\s+(?:"[^"]*"|'[^']*'))?\s*\)/g;
    const reference = /^ {0,3}\[[^\]]+\]:\s*(\S+)/;
    const html = /<(?:a|img)\b[^>]*?\b(?:href|src)=["']([^"']+)["']/gi;

    // Links may wrap across lines, so inline links are matched against the whole page.
    for (const match of content.matchAll(inline)) {
        const line = content.slice(0, match.index).split('\n').length;
        await validateTarget(file, line, match[1]);
    }

    for (const match of content.matchAll(html)) {
        const line = content.slice(0, match.index).split('\n').length;
        await validateTarget(file, line, match[1]);
    }

    for (const [index, line] of lines.entries()) {
        const match = line.match(reference);
        if (match) await validateTarget(file, index + 1, match[1]);
    }
}

// toc.yml is a tree of `name`/`href`/`items`; reading the href values is all that is needed.
async function validateToc(files) {
    const tocPath = path.join(documentationRoot, 'toc.yml');
    let toc;
    try {
        toc = await readFile(tocPath, 'utf8');
    } catch {
        errors.push('Documentation/toc.yml does not exist.');
        return;
    }

    const listed = new Set();
    for (const [index, line] of toc.split('\n').entries()) {
        const match = line.match(/^\s*(?:-\s+)?href:\s*(.+?)\s*$/);
        if (!match) continue;

        const href = match[1].replace(/^['"]|['"]$/g, '');
        if (/^[a-z][a-z0-9+.-]*:/i.test(href)) continue;

        await validateTarget(tocPath, index + 1, href);
        listed.add(path.resolve(documentationRoot, href.split('#')[0]));
    }

    if (listed.size === 0) errors.push('Documentation/toc.yml lists no pages.');

    for (const file of files) {
        if (!listed.has(file)) {
            errors.push(`${relative(file)}: page is not listed in Documentation/toc.yml, so the site has no navigation entry for it.`);
        }
    }
}

const files = await pagesBelow(documentationRoot);
// A checker that examined nothing reports success just as loudly as one that examined everything.
if (files.length === 0) {
    console.error(`Documentation link verification examined 0 pages under ${documentationRoot}; the checker is not effective.`);
    process.exit(1);
}

await validateToc(files);
for (const file of files) await validateLinks(file);

if (errors.length > 0) {
    console.error('Documentation link verification failed:');
    for (const error of errors) console.error(`  - ${error}`);
    process.exit(1);
}

console.log(`Documentation link verification passed for ${files.length} pages: toc entries, relative links and anchors resolve.`);
