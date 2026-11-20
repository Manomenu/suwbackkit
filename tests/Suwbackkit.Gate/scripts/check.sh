#!/usr/bin/env bash
set -euo pipefail
usage() {
    cat <<'USAGE'
usage: check.sh [--quick] | --help
The quality gate. CI runs exactly this script; humans run `just check`, the Claude Code Stop hook runs `--quick`.
  check.sh --quick   build (compiler, analyzers, banned APIs), csharpier and the repository guards (Suwbackkit.Gate)
  check.sh           build, csharpier, every test, pack (each packable project gives a .nupkg and a .snupkg in
                     artifacts/packages), shellcheck and gitleaks
Everything builds once, in Release, and the tests and the pack reuse that build.
The quick gate has a time budget, CHECK_QUICK_LIMIT seconds (default 30): the Stop hook runs it after every turn of an
agent, and a gate that takes minutes gets switched off. Above the budget it fails and says so.
Every step has a hard ceiling (CHECK_STEP_LIMIT seconds overrides them all); a step above it is killed and fails as
"hung or too slow"; without `timeout` on the machine there is no ceiling. A failed step does not stop the others.
Quiet: a green gate prints one line. Otherwise it lists only the failed and skipped steps (passed ones are counted),
after the tail of each failed log.
A step that cannot run (a tool is missing) is SKIP, never PASS; with CI set a SKIP fails the gate, locally the report ends with
"NOT VERIFIED: <steps>". Exit code is non-zero on any failure.
USAGE
}
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
export MSBUILDDISABLENODEREUSE=1
export DOTNET_CLI_USE_MSBUILD_SERVER=0

QUICK=0
case "${1:-}" in
    "") ;;
    --quick) QUICK=1 ;;
    --help)
        usage
        exit 0
        ;;
    *)
        usage >&2
        exit 2
        ;;
esac

LOGS="$(mktemp -d)"
trap 'rm -rf "$LOGS"' EXIT
FAILURE_LINES=25
ORDER=()
declare -A NAME
declare -A SKIPPED
declare -A LIMIT
QUICK_LIMIT="${CHECK_QUICK_LIMIT:-30}"
case "$QUICK_LIMIT" in
    '' | *[!0-9]*)
        echo "check.sh: CHECK_QUICK_LIMIT must be a whole number of seconds, not '$QUICK_LIMIT'" >&2
        exit 2
        ;;
esac

declare_step() { ORDER+=("$1"); NAME["$1"]="$2"; LIMIT["$1"]="$3"; }

run_step() {
    local key="$1" function="$2" start status=0
    start=$SECONDS
    if command -v timeout >/dev/null 2>&1; then
        timeout -k 10 "${CHECK_STEP_LIMIT:-${LIMIT[$key]}}" bash -c "$function" >"$LOGS/$key.log" 2>&1 || status=$?
        if [ "$status" -eq 124 ]; then
            echo "hung or too slow: $function exceeded ${CHECK_STEP_LIMIT:-${LIMIT[$key]}} s and was killed" >>"$LOGS/$key.log"
        fi
    else
        bash -c "$function" >"$LOGS/$key.log" 2>&1 || status=$?
    fi
    echo "$status $((SECONDS - start))" >"$LOGS/$key.result"
}

build_all() { (cd "$ROOT" && dotnet build Suwbackkit.slnx -c Release --nologo -v quiet); }

format_all() { (cd "$ROOT" && dotnet tool restore >/dev/null && dotnet csharpier check .); }

test_all() { (cd "$ROOT" && dotnet test --solution Suwbackkit.slnx -c Release --no-build --no-progress); }

test_guards() { (cd "$ROOT" && dotnet test --project tests/Suwbackkit.Gate/Suwbackkit.Gate.csproj -c Release --no-build --no-progress); }

pack_all() {
    cd "$ROOT"
    rm -rf artifacts/packages
    dotnet pack Suwbackkit.slnx -c Release --no-build --nologo -v quiet
    local project name missing=0
    for project in src/*/*.csproj; do
        name="$(basename "$project" .csproj)"
        for extension in nupkg snupkg; do
            if ! compgen -G "artifacts/packages/$name.[0-9]*.$extension" >/dev/null; then
                echo "pack: $project gave no $extension in artifacts/packages; every project under src/ is a package with symbols (src/Directory.Build.props)"
                missing=1
            fi
        done
    done
    ls -1 artifacts/packages
    return "$missing"
}

shell_scripts() { (cd "$ROOT" && { git ls-files --cached --others --exclude-standard '*.sh'; echo .githooks/pre-commit; } | xargs shellcheck -S warning); }

secrets_in_git() {
    cd "$ROOT"
    if git rev-parse --verify HEAD >/dev/null 2>&1; then
        gitleaks git . --no-banner --redact --log-level warn
    fi
    gitleaks dir . --no-banner --redact --log-level warn --max-target-megabytes 1
}

export ROOT
export -f build_all format_all test_all test_guards pack_all shell_scripts secrets_in_git
if [ "$QUICK" -eq 1 ]; then
    declare_step build "build (compiler, analyzers, banned APIs)" 300
    declare_step format "format C# (csharpier check)" 120
    declare_step test "repository guards (Suwbackkit.Gate)" 120
else
    declare_step build "build (compiler, analyzers, banned APIs, NuGet audit)" 300
    declare_step format "format C# (csharpier check)" 120
    declare_step test "tests (xunit: packages and repository guards)" 300
    declare_step pack "pack (.nupkg and .snupkg per package into artifacts/packages)" 120
    declare_step shellcheck "shell scripts (shellcheck)" 120
    declare_step gitleaks "secrets in git (gitleaks)" 300
    command -v shellcheck >/dev/null 2>&1 || SKIPPED[shellcheck]="shellcheck not installed"
    command -v gitleaks >/dev/null 2>&1 || SKIPPED[gitleaks]="gitleaks not installed"
fi

GATE_START=$SECONDS
PIDS=()
if [ "$QUICK" -eq 1 ]; then
    (
        run_step build build_all
        run_step test test_guards
    ) &
else
    (
        run_step build build_all
        run_step test test_all
        run_step pack pack_all
    ) &
fi
PIDS+=($!)
launch() {
    local key="$1"
    if [ -z "${SKIPPED[$key]+x}" ]; then
        run_step "$key" "$2" &
        PIDS+=($!)
    fi
}
launch format format_all
if [ "$QUICK" -eq 0 ]; then
    launch shellcheck shell_scripts
    launch gitleaks secrets_in_git
fi
for pid in "${PIDS[@]}"; do wait "$pid" || true; done
TOTAL=$((SECONDS - GATE_START))
OVER_BUDGET=0
if [ "$QUICK" -eq 1 ] && [ "$TOTAL" -gt "$QUICK_LIMIT" ]; then OVER_BUDGET=1; fi

FAILED=0
PASSED=0
NOT_VERIFIED=()
REPORT=()
FAILURES=()
for key in "${ORDER[@]}"; do
    if [ -n "${SKIPPED[$key]+x}" ]; then
        if [ -n "${CI:-}" ]; then
            REPORT+=("FAIL        ${NAME[$key]} — ${SKIPPED[$key]} (CI: a skipped step fails)")
            FAILED=1
        else
            REPORT+=("SKIP        ${NAME[$key]} — ${SKIPPED[$key]}")
            NOT_VERIFIED+=("$key")
        fi
        continue
    fi
    if [ ! -f "$LOGS/$key.result" ]; then
        REPORT+=("FAIL        ${NAME[$key]} — did not run")
        FAILED=1
        continue
    fi
    read -r status secs <"$LOGS/$key.result"
    if [ "$status" -eq 0 ]; then
        PASSED=$((PASSED + 1))
    else
        REPORT+=("$(printf 'FAIL %4ss  %s' "$secs" "${NAME[$key]}")")
        FAILURES+=("${NAME[$key]}" "$LOGS/$key.log")
        FAILED=1
    fi
done
if [ "$OVER_BUDGET" -eq 1 ]; then
    REPORT+=("FAIL ${TOTAL}s  the quick gate is over its budget of ${QUICK_LIMIT}s: move the slow step to the full gate or speed it up; a first run on a fresh checkout may exceed it once; a machine that is slow across the board runs with CHECK_QUICK_LIMIT=<seconds>")
    FAILED=1
fi

for ((i = 0; i < ${#FAILURES[@]}; i += 2)); do
    echo
    echo "==================== failed: ${FAILURES[i]} (last $FAILURE_LINES lines) ===================="
    tail -n "$FAILURE_LINES" "${FAILURES[i + 1]}"
done

if [ ${#REPORT[@]} -eq 0 ]; then
    echo "PASS check.sh${1:+ $1}: all $PASSED steps in ${TOTAL}s"
else
    echo
    for line in "${REPORT[@]}"; do
        echo "  $line"
    done
    echo "  $PASSED passed, total ${TOTAL}s"
fi
if [ ${#NOT_VERIFIED[@]} -gt 0 ]; then
    echo "NOT VERIFIED: ${NOT_VERIFIED[*]}"
fi
exit "$FAILED"
