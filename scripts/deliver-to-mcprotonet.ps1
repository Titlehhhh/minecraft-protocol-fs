param(
    [string]$McProtoNetRoot = (Join-Path $PSScriptRoot '..\..\McProtoNet' | Resolve-Path -ErrorAction SilentlyContinue)
)
$ErrorActionPreference = 'Stop'

# Explicit guard for McProtoNetRoot (Finding 2: prevent silent failure if sibling is missing)
if (-not $McProtoNetRoot -or -not (Test-Path $McProtoNetRoot)) {
    throw "McProtoNet root not found - pass -McProtoNetRoot <path> (default expects sibling ..\McProtoNet)"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$staging = Join-Path ([IO.Path]::GetTempPath()) "mcproto-gen-$([guid]::NewGuid().ToString('N'))"

# Packets whose generated code doesn't compile yet (union/metadata codegen gaps).
# Keep in sync with the Exclude list in sandbox/McProtoNet.Sandbox/McProtoNet.Sandbox.csproj;
# the sandbox's NBT-only excludes (RespawnPacket, Configuration DisconnectPacket) are NOT
# carried over here — real McProtoNet has NBT support, so those are deliverable.
$exclude = @(
    'TeamsPacket.cs', 'EntityMetadataPacket.cs', 'ExplosionPacket.cs',
    'MapPacket.cs', 'WindowClickPacket.cs', 'EntityMetadataEntry.cs'
)

# Finding 1: wrap body in try/finally to ensure staging dir is cleaned up on any failure
try {
    dotnet run --project (Join-Path $repoRoot 'minecraft-protocol-fs') -- gen --out $staging
    if ($LASTEXITCODE -ne 0) { throw "generation failed" }

    $target = Join-Path $McProtoNetRoot 'src\McProtoNet.Protocol\Generated'
    if (Test-Path $target) { Remove-Item -Recurse -Force $target }
    New-Item -ItemType Directory -Force $target | Out-Null

    $delivered = 0
    Get-ChildItem -Recurse -File $staging -Filter *.cs |
        Where-Object { $exclude -notcontains $_.Name } |
        ForEach-Object {
            $rel = $_.FullName.Substring($staging.Length + 1)
            $dst = Join-Path $target $rel
            New-Item -ItemType Directory -Force (Split-Path $dst) | Out-Null
            Copy-Item $_.FullName $dst
            $delivered++
        }
    Write-Host "Delivered $delivered generated source file(s) to $target"
}
finally {
    if (Test-Path $staging) {
        Remove-Item -Recurse -Force $staging
    }
}
