#!/usr/bin/env bash
# Linux counterpart of serve-facts.cmd / serve-facts.ps1.
# Starts mcproto-facts' McpServer (REST + HTTP MCP on :5000).
# Use this when you'll query facts many times in a session.
#
#   REST:  http://localhost:5000/api/type/{id}
#          http://localhost:5000/api/schema/{id}
#          http://localhost:5000/api/composition/{id}
#   MCP:   http://localhost:5000/mcp
#   UI:    http://localhost:5000/   (only when started with --ui)
#
# Runs in the FOREGROUND by default: the console stays visible and Ctrl+C stops
# the server. On Windows the .ps1 spawns a separate window for the same effect.
#
#   --ui       also build and serve the web UI bundle (npm install + build on
#              first run, so startup is slower)
#   --window   launch in a separate terminal window instead of the foreground
set -euo pipefail

ui=0
window=0
while [[ $# -gt 0 ]]; do
    case "$1" in
        --ui)     ui=1; shift ;;
        --window) window=1; shift ;;
        -h|--help) sed -n '2,20p' "${BASH_SOURCE[0]}"; exit 0 ;;
        *) echo "unknown option: $1" >&2; exit 2 ;;
    esac
done

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
facts_root="$("$script_dir/_resolve-facts.sh")"
project="$facts_root/src/McpServer/McpServer.csproj"

# BuildClientApp=false: skip the web UI bundle (faster start) unless --ui was passed.
args=(run --project "$project")
[[ $ui -eq 1 ]] || args+=(-p:BuildClientApp=false)

if [[ $ui -eq 1 ]]; then
    echo "McpServer starting (building ClientApp UI on first run)."
    echo "  UI:   http://localhost:5000/"
else
    echo "McpServer starting."
fi
echo "  REST: http://localhost:5000/api   MCP: http://localhost:5000/mcp"

cd "$facts_root"

if [[ $window -eq 1 ]]; then
    title="McpServer ($( [[ $ui -eq 1 ]] && echo 'facts + UI' || echo 'facts' ))"
    # Keep the shell alive after the server stops so errors stay readable.
    inner="dotnet ${args[*]}; echo; echo '[server stopped — press Enter to close]'; read -r"
    if command -v konsole >/dev/null; then
        konsole --title "$title" --workdir "$facts_root" -e bash -lc "$inner" &
    elif command -v x-terminal-emulator >/dev/null; then
        x-terminal-emulator -e bash -lc "cd '$facts_root'; $inner" &
    else
        echo "No terminal emulator found (tried konsole, x-terminal-emulator)." >&2
        echo "Drop --window to run in the foreground instead." >&2
        exit 1
    fi
    echo "Close that window to stop the server."
    exit 0
fi

echo "Ctrl+C to stop."
exec dotnet "${args[@]}"
