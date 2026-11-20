set shell := ["bash", "-euo", "pipefail", "-c"]

[private]
default:
    @just --list --unsorted

# The whole gate (suwgate check): guards, build, csharpier, tests, pack, criteria, shellcheck, gitleaks; the same CI runs
[group('maintenance')]
check:
    @pnpm exec suwgate check

# The quick gate (suwgate check --quick): the guards and what changed since master; the Claude Code Stop hook runs it
[group('maintenance')]
check-quick:
    @pnpm exec suwgate check --quick

# Format the code with CSharpier
[group('maintenance')]
fmt:
    dotnet tool restore >/dev/null && dotnet csharpier format .

# Pack every package under src/ into artifacts/packages (.nupkg and .snupkg); the version comes from the git tag (MinVer)
[group('dev')]
pack:
    rm -rf artifacts/packages
    dotnet pack Suwbackkit.slnx -c Release

# Install suwgate, the tools and the packages from the lockfiles, and enable the git hooks
[group('maintenance')]
sync:
    git config core.hooksPath .githooks
    pnpm install --frozen-lockfile
    dotnet tool restore
    dotnet restore Suwbackkit.slnx
