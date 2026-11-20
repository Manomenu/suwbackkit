#!/usr/bin/env bash
set -euo pipefail

input="$(cat)"
path="$(printf '%s' "$input" | grep -o '"file_path"[[:space:]]*:[[:space:]]*"[^"]*"' | head -n 1 | sed 's/^[^:]*:[[:space:]]*"//; s/"$//')"
path="$(printf '%s' "$path" | tr '\134' '/')"
[ -n "$path" ] || exit 0

project="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "$0")/../../.." && pwd)}"
project="$(printf '%s' "$project" | tr '\134' '/')"
case "$path" in
    "$project"/*) ;;
    *) exit 0 ;;
esac

if [ "${ALLOW_GATE_EDIT:-}" != "1" ]; then
    case "$path" in
        */tests/Suwbackkit.Gate/* | */.claude/settings.json | */.github/workflows/* | */.editorconfig | */BannedSymbols.txt | \
            */Directory.Build.props)
            echo "Blocked: $path is part of the gate (a check, a hook or a lint config), which an agent does not change on its own." >&2
            echo "Do not work around it: ask the user to start a session with ALLOW_GATE_EDIT=1 claude and give them the exact change to make there (the file, what to change, why)." >&2
            exit 2
            ;;
    esac
fi

case "$path" in
    */packages.lock.json)
        echo "Blocked: $path is a generated lockfile. Change Directory.Packages.props and run: dotnet restore Suwbackkit.slnx" >&2
        exit 2
        ;;
esac
exit 0
