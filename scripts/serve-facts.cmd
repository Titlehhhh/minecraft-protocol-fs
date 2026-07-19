@echo off
rem Starts PacketGenerator's McpServer in its own window (REST + HTTP MCP on :5000).
rem Close that window to stop the server.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0serve-facts.ps1" %*
exit /b %errorlevel%
