#!/usr/bin/env bash
# Linux counterpart of build-latest.ps1.
# Serialises builds behind a lock file so parallel CLI/MCP invocations don't
# race on the same obj/bin. Build output is swallowed on success to keep the
# caller's stdout clean; on failure it is replayed to stderr.
set -euo pipefail

usage() {
    echo "usage: ${0##*/} --project <csproj> [--lock-name <name>]" >&2
    exit 2
}

project=""
lock_name="mcproto-facts-build"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --project)   project="${2-}"; shift 2 ;;
        --lock-name) lock_name="${2-}"; shift 2 ;;
        *)           usage ;;
    esac
done

[[ -n "$project" ]] || usage

tmp="${TMPDIR:-/tmp}"
lock_file="$tmp/$lock_name.lock"
build_log="$(mktemp "$tmp/mcproto-facts-build-XXXXXX.log")"
trap 'rm -f "$build_log"' EXIT

exec 9>"$lock_file"
flock 9

if ! dotnet build "$project" -maxcpucount:1 -nologo -v:q >"$build_log" 2>&1; then
    cat "$build_log" >&2
    exit 1
fi
