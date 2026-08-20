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

# Packets whose generated code doesn't compile yet (metadata codegen gaps, missing types).
# Keep in sync with the Exclude list in sandbox/McProtoNet.Sandbox/McProtoNet.Sandbox.csproj;
# the sandbox's NBT-only excludes (RespawnPacket, Configuration DisconnectPacket) are NOT
# carried over here — real McProtoNet has NBT support, so those are deliverable.
#
# Unions/*.cs are held back until McProtoNet references Dunet: the generated union carries
# [Union], which that package's generator turns into the case hierarchy. The reference must be
# plain — since Dunet 1.11 the package ships a real runtime assembly holding the attribute, so
# PrivateAssets="all" starves it and anything reflecting over the type fails at run time. Once it is referenced, drop TeamAction.cs,
# TeamsPacket.cs and EntityMetadataEntry.cs from this list — they compile in the sandbox today.
# EntityMetadataValue.cs stays until Slot, Particle and the registry variants are modelled.
# UnionShapeProbe.cs is a codegen fixture, not protocol: it exists so the sandbox compiles the
# union shapes EntityMetadataValue is built from. It is never delivered.
$exclude = @(
    'TeamsPacket.cs', 'EntityMetadataPacket.cs', 'ExplosionPacket.cs',
    'MapPacket.cs', 'WindowClickPacket.cs', 'EntityMetadataEntry.cs',
    'TeamAction.cs', 'EntityMetadataValue.cs', 'UnionShapeProbe.cs'
)

# One generated file declares one type, named after the file, so an excluded file name is the
# name of a type the delivered set must never mention. Returns file -> type pairs to report.
function Find-DanglingReferences {
    param([IO.FileInfo[]]$Delivered, [IO.FileInfo[]]$Held, [string]$Root)

    $heldTypes = $Held |
        ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_.Name) } |
        Sort-Object -Unique

    if (-not $heldTypes) { return @() }

    $pattern = '\b(' + (($heldTypes | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')\b'

    foreach ($file in $Delivered) {
        $hits = ([regex]::Matches((Get-Content -Raw $file.FullName), $pattern) |
            ForEach-Object { $_.Value } | Sort-Object -Unique)
        foreach ($hit in $hits) {
            [pscustomobject]@{
                File = $file.FullName.Substring($Root.Length + 1)
                Type = $hit
            }
        }
    }
}

# Finding 1: wrap body in try/finally to ensure staging dir is cleaned up on any failure
try {
    dotnet run --project (Join-Path $repoRoot 'minecraft-protocol-fs') -- gen --out $staging
    if ($LASTEXITCODE -ne 0) { throw "generation failed" }

    $generated = @(Get-ChildItem -Recurse -File $staging -Filter *.cs)
    $held = @($generated | Where-Object { $exclude -contains $_.Name })
    $shipped = @($generated | Where-Object { $exclude -notcontains $_.Name })

    # A partial delivery that does not compile is worse than no delivery: refuse before the
    # target directory is touched, so a broken set leaves McProtoNet exactly as it was.
    $dangling = @(Find-DanglingReferences -Delivered $shipped -Held $held -Root $staging)
    if ($dangling.Count -gt 0) {
        $lines = $dangling | ForEach-Object { "  {0} references {1}" -f $_.File, $_.Type }
        throw (
            "delivery refused: $($dangling.Count) reference(s) in the delivered set point at " +
            "excluded file(s), so McProtoNet would not compile:`n" + ($lines -join "`n") +
            "`nEither stop excluding those files or stop generating the references.")
    }

    $target = Join-Path $McProtoNetRoot 'src\McProtoNet.Protocol\Generated'
    if (Test-Path $target) { Remove-Item -Recurse -Force $target }
    New-Item -ItemType Directory -Force $target | Out-Null

    $delivered = 0
    foreach ($file in $shipped) {
        $rel = $file.FullName.Substring($staging.Length + 1)
        $dst = Join-Path $target $rel
        New-Item -ItemType Directory -Force (Split-Path $dst) | Out-Null
        Copy-Item $file.FullName $dst
        $delivered++
    }
    Write-Host "Delivered $delivered generated source file(s) to $target"
}
finally {
    if (Test-Path $staging) {
        Remove-Item -Recurse -Force $staging
    }
}
