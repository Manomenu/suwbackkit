# suwbackkit

Shared .NET building blocks for the backends of our apps, as NuGet packages. Checked by [suwgate](https://github.com/Manomenu/suwgate) (`just check`).

| Package | What it gives an app |
| --- | --- |
| [Suwbackkit.Server](src/Suwbackkit.Server/README.md) | `AddServerDefaults()` and `MapServerDefaults()`: JSON console logging, ProblemDetails with `traceId`, OpenAPI 3.1, health checks (`/health`, `/alive`), `MapHealthStatus()` for the `/api/health` the contracts already publish, and `AddContractSchemas()`: OpenAPI schemas that say what the JSON always holds |
| [Suwbackkit.Postgres](src/Suwbackkit.Postgres/README.md) | `AddDatabase()`: one `NpgsqlDataSource`, grate migrations on start, a database health check |

What comes next is in [TODO.md](TODO.md), why it is built this way in [docs/architecture.md](docs/architecture.md).

## Work on it

Setup and commands: [docs/development.md](docs/development.md). The rules for every change: [AGENTS.md](AGENTS.md).

## Release

The version comes from the git tag (MinVer): tag `v0.1.0` builds `0.1.0`; every commit after it builds `0.1.1-alpha.0.<n>`.

```sh
git tag v0.1.0
git push origin v0.1.0
```

CI runs the gate on the tag, packs, and, once nuget.org trusted publishing is set up (see docs/architecture.md, "Publishing"), pushes every package and its symbols to nuget.org.

## License

[MIT](LICENSE)
