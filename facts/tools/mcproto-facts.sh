#!/usr/bin/env bash
# Linux counterpart of mcproto-facts.cmd — clean-stdout CLI over protocol facts.
#   tools/mcproto-facts.sh stats --format toon
#   tools/mcproto-facts.sh packet play.toClient.teams --format toon
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$root/src/McProtoFacts.Cli/McProtoFacts.Cli.csproj"

# Run from the repo root so this repo's global.json (SDK pin) applies.
cd "$root"

"$root/tools/build-latest.sh" --project "$project" --lock-name mcproto-facts-build

exec dotnet run --project "$project" --no-build --no-restore -- "$@"
