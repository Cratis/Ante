#!/bin/bash

# Markdown Verification Script
# Runs the same checks as the Markdown Verification workflow, so that what CI
# checks and what a contributor checks locally cannot drift apart.
#
#   1. markdownlint on every page (blocking)
#   2. toc.yml entries, relative links and anchors (blocking)
#   3. external http(s) links (reported, never blocking - a third-party outage
#      must not fail a pull request)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# Pinned so that a new major version cannot silently change what is checked.
MARKDOWNLINT_VERSION="0.23.3"
LINKINATOR_VERSION="8.0.2"

printf '%s\n' \
    "==========================================" \
    "Markdown Verification" \
    "==========================================" \
    ""

# Every step addresses paths from the repository root, whether this script is run
# from there or from the Documentation folder.
cd "$ROOT_DIR"

echo "Working directory: $PWD"
echo ""

if ! command -v npx &> /dev/null || ! command -v node &> /dev/null; then
    echo "Error: node and npx are not installed. Please install Node.js and npm."
    exit 1
fi

# Each step captures its own exit code and the run continues, so that one failing
# step does not leave the state of the others unreported.

printf '%s\n' "Step 1: Running markdownlint..." ""

if npx --yes "markdownlint-cli2@$MARKDOWNLINT_VERSION" "Documentation/**/*.{md,mdx}"; then
    LINT_EXIT_CODE=0
else
    LINT_EXIT_CODE=$?
fi
echo ""

printf '%s\n' "Step 2: Verifying toc.yml entries, relative links and anchors..." ""

if node "$SCRIPT_DIR/verify-links.mjs"; then
    LINKS_EXIT_CODE=0
else
    LINKS_EXIT_CODE=$?
fi
echo ""

printf '%s\n' "Step 3: Checking external links (non-blocking)..." ""

# linkinator serves the pages from a local web server, so every link into this
# folder resolves to http://127.0.0.1:<port>/Documentation/... and is re-checked
# here as a side effect; step 2 is the authority for those and blocks on them.
# Site-absolute links (/arc/..., /chronicle/...) resolve only on the aggregated
# documentation site and are skipped. A failure here is only reported.
set +e
NO_COLOR=1 npx --yes "linkinator@$LINKINATOR_VERSION" \
    "Documentation/**/*.{md,mdx}" \
    --markdown \
    --recurse \
    --directory-listing \
    --verbosity error \
    --retry-errors \
    --timeout 30000 \
    --status-code "403:ok" \
    --skip '^https?://(localhost|127\.0\.0\.1):[0-9]+/(?!Documentation/)'
EXTERNAL_EXIT_CODE=$?
set -e

if [ "$EXTERNAL_EXIT_CODE" -ne 0 ]; then
    echo "::warning title=External links::Some external links in Documentation could not be verified. This does not fail the run."
    echo "! External link check reported problems (exit code $EXTERNAL_EXIT_CODE); not blocking."
fi
echo ""

printf '%s\n' \
    "==========================================" \
    "Summary" \
    "=========================================="

if [ "$LINT_EXIT_CODE" -eq 0 ] && [ "$LINKS_EXIT_CODE" -eq 0 ]; then
    echo "✓ All blocking checks passed!"
    [ "$EXTERNAL_EXIT_CODE" -ne 0 ] && echo "! External links need attention (non-blocking)."
    exit 0
fi

echo "✗ Some checks failed:"
[ "$LINT_EXIT_CODE" -ne 0 ] && echo "  - Markdown linting"
[ "$LINKS_EXIT_CODE" -ne 0 ] && echo "  - toc.yml, relative links and anchors"
exit 1
