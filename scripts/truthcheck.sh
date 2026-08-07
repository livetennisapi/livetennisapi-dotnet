#!/bin/sh
# truthcheck.sh — pins this repo's copy to Live Tennis API ground truth.
# POSIX sh, no dependencies beyond git + grep. Run from anywhere in the repo.
set -u

cd "$(dirname "$0")/.." || exit 1

fail=0

# Tracked text files, minus this script (its patterns match themselves), the
# CHANGELOG (its history entries may describe old, since-corrected copy) and
# binary-ish assets.
files=$(git ls-files | grep -v '^CHANGELOG\.md$' | grep -v '^scripts/truthcheck\.sh$' | grep -viE '\.(png|jpg|jpeg|gif|ico|snk)$')

forbid() {
    pattern="$1"
    why="$2"
    # shellcheck disable=SC2086
    hits=$(printf '%s\n' "$files" | while IFS= read -r f; do
        [ -f "$f" ] && grep -niE "$pattern" "$f" /dev/null 2>/dev/null
    done)
    if [ -n "$hits" ]; then
        echo "FORBIDDEN ($why):"
        printf '%s\n' "$hits"
        fail=1
    fi
}

require() {
    pattern="$1"
    why="$2"
    # shellcheck disable=SC2086
    found=$(printf '%s\n' "$files" | while IFS= read -r f; do
        [ -f "$f" ] && grep -liE "$pattern" "$f" 2>/dev/null
    done)
    if [ -z "$found" ]; then
        echo "MISSING ($why): no tracked file matches /$pattern/"
        fail=1
    fi
}

# --- Forbidden claims -------------------------------------------------------
# Stale ULTRA day quota (was 100k before 2026-08-06; now 500,000).
forbid '100[,.]?000[^0-9].{0,40}(/day|per day|day quota|daily)|(/day|per day|day quota|daily).{0,40}100[,.]?000([^0-9]|$)|100k.{0,20}(/day|per day|daily)' 'stale 100,000/day quota'
# Stale FREE day quota (was 1,000/day before 2026-08-06; now 100/day).
forbid 'free[^.]{0,60}1[,.]?000[^0-9,]{0,20}(/day|per day|requests/day|daily)|free tier[^.]{0,40}1k' 'FREE tier is 100/day, not 1,000'
# Wrong docs URL (docs live at docs.livetennisapi.com).
forbid 'livetennisapi\.com/docs' 'docs are at docs.livetennisapi.com, never livetennisapi.com/docs'
# Personal handle must not appear in repo metadata or copy.
forbid 'bensynapse' 'use the org identity, not a personal handle'
# The daily reset is an absolute local-midnight-derived instant.
forbid 'midnight UTC' 'daily reset is resets_at, an absolute instant — never "midnight UTC"'

# --- Required truths (this repo states quotas) ------------------------------
require '100( requests)?/day' 'FREE quota copy (100/day)'
require 'docs\.livetennisapi\.com' 'canonical docs URL'

if [ "$fail" -ne 0 ]; then
    echo "truthcheck: FAILED"
    exit 1
fi

echo "truthcheck: OK"
exit 0
