# AGENTS.md

Rules for anyone — human or AI agent — changing this repository. `CLAUDE.md` only points here;
this file is the one to edit. What it is: [README.md](README.md). Setup: [docs/development.md](docs/development.md). Not built yet: [TODO.md](TODO.md).

Read @AGENTS.shared.md first: the rules every repository of the stack shares. This file adds this repository's own and wins where it says otherwise.

This repository is a set of NuGet packages, public on GitHub and nuget.org: every public type is a promise to the apps
that use it, and every published version stays forever.

## Commands

- suwgate runs as `pnpm exec suwgate`, configured in `suwgate.yml`; a recipe calls it. `suwgate --help` describes every command.
- A release is a tag `v<major>.<minor>.<patch>` on `master`, pushed once the commit's CI is green; CI publishes it with nuget.org trusted publishing, never with an API key.

## Rules

- Code comes here only when it is generalised from at least two apps that need it today ([docs/architecture.md](docs/architecture.md)). One app's need stays in that app.
- A package never depends on an app, and nothing here knows which apps use it.
- A change to a public type is a change to every app that uses it: semantic versioning decides the next tag (a breaking change raises the major, or the minor while it is 0.x), and the change says which.
- Names are in technical English. A package is `Suwbackkit.<Area>`.
- `dynamic` is banned like the escape hatches of the shared list.

## Specs

- Each acceptance criterion has an id `AC-<feature>-<n>`; a criterion is guarded by a test that names its id (`[Trait("criterion", "<id>")]`).
