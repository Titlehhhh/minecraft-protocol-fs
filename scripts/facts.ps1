# One-shot protocol fact lookup via McProtoFacts' CLI.
# Use this for a single query. For many queries in a session, run serve-facts.cmd instead.
#
# Examples:
#   scripts\facts.cmd type entityMetadata --format toon
#   scripts\facts.cmd packet play.toClient.teams --format toon
#   scripts\facts.cmd composition play.toClient.map --format json
#   scripts\facts.cmd packets --filter metadata --format json
$ErrorActionPreference = 'Stop'

$factsRoot = & (Join-Path $PSScriptRoot '_resolve-facts.ps1')
$cli = Join-Path $factsRoot 'tools\mcproto-facts.cmd'

# Run from the McProtoFacts root so its global.json (SDK) applies, not this repo's.
Push-Location $factsRoot
try {
    & $cli @args
    $code = $LASTEXITCODE
}
finally {
    Pop-Location
}
exit $code
