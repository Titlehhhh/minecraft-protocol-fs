#!/usr/bin/env bash
# Linux counterpart of mcproto-facts-mcp.cmd — stdio MCP server over protocol facts.
# Point an MCP client at this script; it speaks JSON-RPC on stdin/stdout.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$root/src/McProtoFacts.McpStdio/McProtoFacts.McpStdio.csproj"

# Run from the repo root so this repo's global.json (SDK pin) applies.
cd "$root"

"$root/tools/build-latest.sh" --project "$project" --lock-name mcproto-facts-build

exec dotnet run --project "$project" --no-build --no-restore -- "$@"
