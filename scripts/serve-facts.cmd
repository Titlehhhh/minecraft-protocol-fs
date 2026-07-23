@echo off
rem Starts McProtoFacts' McpServer in its own window (REST + HTTP MCP on :5000).
rem Pass -Ui to also build and serve the web UI at http://localhost:5000/ (slower first start).
rem Close that window to stop the server.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0serve-facts.ps1" %*
exit /b %errorlevel%
