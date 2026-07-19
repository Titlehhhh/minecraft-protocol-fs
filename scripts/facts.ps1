# One-shot protocol fact lookup via PacketGenerator's CLI.
# Use this for a single query. For many queries in a session, run serve-facts.cmd instead.
#
# Examples:
#   scripts\facts.cmd type entity_metadata --format toon
#   scripts\facts.cmd packet play.toClient.teams --format toon
#   scripts\facts.cmd composition play.toClient.map --format json
#   scripts\facts.cmd packets --filter metadata --format json
$ErrorActionPreference = 'Stop'

$pg = & (Join-Path $PSScriptRoot '_resolve-packetgen.ps1')
$packetgen = Join-Path $pg 'tools\packetgen.cmd'

& $packetgen @args
exit $LASTEXITCODE
