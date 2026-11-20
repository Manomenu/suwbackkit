# Development

## Requirements

- The .NET SDK from `global.json` (tests run on Microsoft.Testing.Platform) and `just`.
- Node 24 and pnpm 11 (`corepack enable`): the gate is the npm package `@maniumek/suwgate`, the only thing `package.json` installs.
- For the full gate: `shellcheck`, `gitleaks`, `unzip` (the pack step reads each package's dependencies), and `python3` with `curl`
  (the `criteria` step downloads its pinned `vdoc.py` once). A missing tool is SKIP locally, a failure in CI, and the report ends with
  `NOT VERIFIED: <steps>`.
- Docker, or Podman with `DOCKER_HOST` pointing at its socket: the PostgreSQL tests start `postgres:17.11` with Testcontainers. Without
  either they are skipped with a message saying so; on CI they fail.

Once per clone: `just sync` (the git hooks, suwgate, CSharpier and the NuGet packages from the lockfiles).

## Everyday commands

```sh
just check        # pnpm exec suwgate check: the whole gate, the same CI runs
just check-quick  # pnpm exec suwgate check --quick: the guards and what changed since master
just fmt          # format with CSharpier
just pack         # packages into artifacts/packages
```

What the gate runs and why: [architecture.md](architecture.md), "The gate". A finding that is a true false positive gets an exception
with its reason in `suwgate.yml`; anything else is fixed in the change.
