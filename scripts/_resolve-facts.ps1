# Resolves the in-repo McProtoFacts checkout (moved here 2026-08-04,
# formerly the sibling mcproto-facts/PacketGenerator repository).
$ErrorActionPreference = 'Stop'

$factsRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'facts'

if (-not (Test-Path (Join-Path $factsRoot 'tools\mcproto-facts.cmd'))) {
    Write-Error "McProtoFacts not found at $factsRoot. Run: git submodule update --init facts/minecraft-data"
    exit 1
}

(Resolve-Path $factsRoot).Path
