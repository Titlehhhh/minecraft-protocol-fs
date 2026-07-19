# Starts PacketGenerator's McpServer in its OWN terminal window.
# Use this when you'll query facts many times in a session (REST + HTTP MCP).
# Close that window to stop the server.
#
#   REST:  http://localhost:5000/api/type/{id}
#          http://localhost:5000/api/schema/{id}
#          http://localhost:5000/api/composition/{id}
#   MCP:   http://localhost:5000/mcp
#   UI:    http://localhost:5000/   (only when started with -Ui)
#
# -Ui  Also build and serve the web UI bundle (npm install + build on first run,
#      so startup is slower). Without it the client app is skipped for a faster start.
param([switch]$Ui)

$ErrorActionPreference = 'Stop'

$pg = & (Join-Path $PSScriptRoot '_resolve-packetgen.ps1')
$proj = Join-Path $pg 'src\McpServer\McpServer.csproj'

# BuildClientApp=false: skip the web UI bundle (faster start) unless -Ui was passed.
$clientFlag = if ($Ui) { '' } else { ' -p:BuildClientApp=false' }
$title = if ($Ui) { 'McpServer (facts + UI)' } else { 'McpServer (facts)' }
$inner = "dotnet run --project `"$proj`"$clientFlag"

# /k keeps the window open after the server stops so errors stay visible; close it to stop.
Start-Process -FilePath 'cmd.exe' -ArgumentList "/k title $title && $inner" -WorkingDirectory $pg

Write-Host "McpServer starting in a separate window$(if ($Ui) { ' (building ClientApp UI on first run)' })."
if ($Ui) { Write-Host "  UI:   http://localhost:5000/" }
Write-Host "  REST: http://localhost:5000/api   MCP: http://localhost:5000/mcp"
Write-Host "Close that window to stop the server."
