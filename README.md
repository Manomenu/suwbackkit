# suwbackkit

Shared .NET building blocks for the backends of our apps, as NuGet packages: first `Suwbackkit.Server` (server defaults: logging, health checks, ProblemDetails, OpenAPI, Npgsql with Dapper and grate), then `Suwbackkit.Cli` (a CLI and SDK skeleton). Checked by `just check` today, by `suwgate check` once it is published.

Status: a skeleton. `Suwbackkit.Server` holds no defaults yet; what comes next is in [TODO.md](TODO.md), why it is built this way in [docs/architecture.md](docs/architecture.md).

## Work on it

Needs the .NET SDK from `global.json`, `just`, and for the full gate `shellcheck` and `gitleaks`.

```sh
just sync         # tools, packages from the lockfiles, git hooks
just check        # the gate CI runs: build, csharpier, tests, pack, shellcheck, gitleaks
just check-quick  # build, csharpier, repository guards
just fmt          # format with CSharpier
just pack         # packages into artifacts/packages
```

## Release

The version comes from the git tag (MinVer): tag `v0.1.0` builds `0.1.0`; every commit after it builds `0.1.1-alpha.0.<n>`.

```sh
git tag v0.1.0
git push origin v0.1.0
```

CI runs the gate on the tag and, once nuget.org trusted publishing is set up (see docs/architecture.md, "Publishing"), pushes every package and its symbols to nuget.org.

## License

[MIT](LICENSE)
