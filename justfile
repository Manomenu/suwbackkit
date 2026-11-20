set shell := ["bash", "-euo", "pipefail", "-c"]

[private]
default:
    @just --list --unsorted

# The whole gate: build with analyzers, csharpier, tests, pack, shellcheck, gitleaks (the same CI runs)
[group('maintenance')]
check:
    @./tests/Suwbackkit.Gate/scripts/check.sh

# The quick gate: build, csharpier and the repository guards (what the Claude Code Stop hook runs)
[group('maintenance')]
check-quick:
    @./tests/Suwbackkit.Gate/scripts/check.sh --quick

# Format the code with CSharpier
[group('maintenance')]
fmt:
    dotnet tool restore >/dev/null && dotnet csharpier format .

# Pack every package under src/ into artifacts/packages (.nupkg and .snupkg); the version comes from the git tag (MinVer)
[group('dev')]
pack:
    rm -rf artifacts/packages
    dotnet pack Suwbackkit.slnx -c Release

# Install tools and packages from the lockfiles, and enable the git hooks
[group('maintenance')]
sync:
    git config core.hooksPath .githooks
    dotnet tool restore
    dotnet restore Suwbackkit.slnx
