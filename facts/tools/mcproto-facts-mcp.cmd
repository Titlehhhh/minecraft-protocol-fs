@echo off
setlocal
set "ROOT=%~dp0.."
set "PROJECT=%ROOT%\src\McProtoFacts.McpStdio\McProtoFacts.McpStdio.csproj"

powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\tools\build-latest.ps1" -Project "%PROJECT%" -MutexName "Global\McProtoFactsBuild"
if errorlevel 1 exit /b %errorlevel%

dotnet run --project "%PROJECT%" --no-build --no-restore -- %*
exit /b %errorlevel%
