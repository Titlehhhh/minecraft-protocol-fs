# Starts PacketGenerator's McpServer in its OWN terminal window.
# Use this when you'll query facts many times in a session (REST + HTTP MCP).
# Close that window to stop the server.
#
#   REST:  http://localhost:5000/api/type/{id}
#          http://localhost:5000/api/schema/{id}
#          http://localhost:5000/api/composition/{id}
#   MCP:   http://localhost:5000/mcp
#   UI:    http://localhost:5000/
$ErrorActionPreference = 'Stop'

$pg = & (Join-Path $PSScriptRoot '_resolve-packetgen.ps1')
$proj = Join-Path $pg 'src\McpServer\McpServer.csproj'

# BuildClientApp=false: we only need REST/MCP facts, not the web UI bundle (faster start).
$inner = "dotnet run --project `"$proj`" -p:BuildClientApp=false"

# /k keeps the window open after the server stops so errors stay visible; close it to stop.
Start-Process -FilePath 'cmd.exe' -ArgumentList "/k title McpServer (facts) && $inner" -WorkingDirectory $pg

Write-Host "McpServer starting in a separate window."
Write-Host "  REST: http://localhost:5000/api   MCP: http://localhost:5000/mcp   UI: http://localhost:5000/"
Write-Host "Close that window to stop the server."
