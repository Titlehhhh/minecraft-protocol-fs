#!/usr/bin/env bash
# Linux counterpart of facts.cmd / facts.ps1.
# One-shot protocol fact lookup via mcproto-facts' CLI.
# Use this for a single query. For many queries in a session, run serve-facts.sh instead.
#
# Examples:
#   scripts/facts.sh type entityMetadata --format toon
#   scripts/facts.sh packet play.toClient.teams --format toon
#   scripts/facts.sh composition play.toClient.map --format json
#   scripts/facts.sh packets --filter metadata --format json
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
facts_root="$("$script_dir/_resolve-facts.sh")"

cli="$facts_root/tools/mcproto-facts.sh"
if [[ ! -f "$cli" ]]; then
    echo "Found mcproto-facts at $facts_root but tools/mcproto-facts.sh is missing." >&2
    exit 1
fi

# Run from the mcproto-facts root so its global.json (SDK) applies, not this repo's.
cd "$facts_root"
exec "$cli" "$@"
