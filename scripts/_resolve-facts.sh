#!/usr/bin/env bash
# Resolves the in-repo McProtoFacts checkout (moved here 2026-08-04).
set -euo pipefail
facts_root="$(cd "$(dirname "$0")/.." && pwd)/facts"
if [ ! -f "$facts_root/tools/mcproto-facts.sh" ] && [ ! -f "$facts_root/tools/mcproto-facts.cmd" ] && [ ! -f "$facts_root/tools/packetgen.cmd" ]; then
  echo "McProtoFacts not found at $facts_root. Run: git submodule update --init facts/minecraft-data" >&2
  exit 1
fi
echo "$facts_root"
