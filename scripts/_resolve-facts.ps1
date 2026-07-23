# Resolves the McProtoFacts checkout (formerly PacketGenerator) that provides
# prepared protocol facts.
# Order: $env:MCPROTO_FACTS_ROOT -> $env:PACKETGEN_ROOT (legacy)
#        -> ../mcprotonet-workspace/{mcproto-facts,PacketGenerator} -> ../{mcproto-facts,PacketGenerator}.
# Kept out of committed docs so this repo can go public without leaking a personal path.
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

$candidates = @()
if ($env:MCPROTO_FACTS_ROOT) { $candidates += $env:MCPROTO_FACTS_ROOT }
if ($env:PACKETGEN_ROOT)     { $candidates += $env:PACKETGEN_ROOT }
foreach ($name in 'mcproto-facts', 'PacketGenerator') {
    $candidates += (Join-Path $repoRoot "..\mcprotonet-workspace\$name")
    $candidates += (Join-Path $repoRoot "..\$name")
}

foreach ($c in $candidates) {
    if (-not $c) { continue }
    foreach ($cli in 'tools\mcproto-facts.cmd', 'tools\packetgen.cmd') {
        if (Test-Path (Join-Path $c $cli)) {
            return (Resolve-Path $c).Path
        }
    }
}

Write-Error @"
McProtoFacts checkout not found.
Set MCPROTO_FACTS_ROOT to the mcproto-facts repo, e.g.:
  `$env:MCPROTO_FACTS_ROOT = 'C:\path\to\mcproto-facts'
Tried:
$($candidates -join "`n")
"@
exit 1
