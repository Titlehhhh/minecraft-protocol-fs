# Resolves the PacketGenerator checkout that provides prepared protocol facts.
# Order: $env:PACKETGEN_ROOT -> ../mcprotonet-workspace/PacketGenerator -> ../PacketGenerator.
# Kept out of committed docs so this repo can go public without leaking a personal path.
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

$candidates = @()
if ($env:PACKETGEN_ROOT) { $candidates += $env:PACKETGEN_ROOT }
$candidates += (Join-Path $repoRoot '..\mcprotonet-workspace\PacketGenerator')
$candidates += (Join-Path $repoRoot '..\PacketGenerator')

foreach ($c in $candidates) {
    if ($c -and (Test-Path (Join-Path $c 'tools\packetgen.cmd'))) {
        return (Resolve-Path $c).Path
    }
}

Write-Error @"
PacketGenerator checkout not found.
Set PACKETGEN_ROOT to the PacketGenerator repo, e.g.:
  `$env:PACKETGEN_ROOT = 'C:\path\to\PacketGenerator'
Tried:
$($candidates -join "`n")
"@
exit 1
