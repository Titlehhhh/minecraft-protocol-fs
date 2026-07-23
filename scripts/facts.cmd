@echo off
rem One-shot protocol fact lookup. Passes all args through to McProtoFacts' CLI.
rem   scripts\facts.cmd type entity_metadata --format toon
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0facts.ps1" %*
exit /b %errorlevel%
