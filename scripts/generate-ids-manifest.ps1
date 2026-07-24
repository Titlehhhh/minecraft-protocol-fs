param([string]$BaseUrl = 'http://localhost:5000')
$ErrorActionPreference = 'Stop'
$dst = Join-Path (Split-Path -Parent $PSScriptRoot) 'minecraft-protocol-fs\Spec\protocol-ids.json'
try { Invoke-RestMethod "$BaseUrl/api/stats" | Out-Null }
catch { throw "McProtoFacts server is not reachable at $BaseUrl - start it with scripts\serve-facts.cmd" }
$out = [ordered]@{
    comment = 'Generated from McProtoFacts /api/packets/{ns}/{dir} (protodef "packet" mappers of minecraft-data). Regenerate: scripts\generate-ids-manifest.ps1 with serve-facts running.'
    packets = [ordered]@{}
}
foreach ($ns in 'handshaking', 'status', 'login', 'configuration', 'play') {
    foreach ($dir in 'toClient', 'toServer') {
        $packets = try { Invoke-RestMethod "$BaseUrl/api/packets/$ns/$dir" } catch { continue }
        foreach ($p in $packets | Sort-Object Id) {
            $out.packets[$p.Id] = @($p.PacketIds | ForEach-Object {
                [ordered]@{ from = $_.From; to = $_.To; id = $_.HexId } })
        }
    }
}
$out | ConvertTo-Json -Depth 5 | Set-Content $dst -Encoding utf8
Write-Host "Wrote $($out.packets.Count) packet id entries to $dst"
