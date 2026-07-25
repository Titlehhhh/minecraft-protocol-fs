#!/usr/bin/env bash
# Linux counterpart of _resolve-facts.ps1.
# Resolves the mcproto-facts checkout (formerly PacketGenerator) that provides
# prepared protocol facts. Prints its absolute path on stdout.
# Order: $MCPROTO_FACTS_ROOT -> $PACKETGEN_ROOT (legacy)
#        -> ../mcprotonet-workspace/{mcproto-facts,PacketGenerator} -> ../{...}
# Kept out of committed docs so this repo can go public without leaking a personal path.
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(dirname "$script_dir")"

candidates=()
[[ -n "${MCPROTO_FACTS_ROOT-}" ]] && candidates+=("$MCPROTO_FACTS_ROOT")
[[ -n "${PACKETGEN_ROOT-}" ]]     && candidates+=("$PACKETGEN_ROOT")
for name in mcproto-facts PacketGenerator; do
    candidates+=("$repo_root/../mcprotonet-workspace/$name" "$repo_root/../$name")
done

for candidate in "${candidates[@]}"; do
    [[ -n "$candidate" && -d "$candidate" ]] || continue
    for cli in tools/mcproto-facts.sh tools/mcproto-facts.cmd tools/packetgen.cmd; do
        if [[ -f "$candidate/$cli" ]]; then
            (cd "$candidate" && pwd -P)
            exit 0
        fi
    done
done

{
    echo "mcproto-facts checkout not found."
    echo "Set MCPROTO_FACTS_ROOT to the mcproto-facts repo, e.g.:"
    echo "  export MCPROTO_FACTS_ROOT=/path/to/mcproto-facts"
    echo "Tried:"
    printf '  %s\n' "${candidates[@]}"
} >&2
exit 1
